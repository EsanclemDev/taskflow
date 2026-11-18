
import {
  Component,
  OnInit,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  CdkDragDrop,
  DragDropModule
} from '@angular/cdk/drag-drop';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import {
  Task,
  TaskStatus,
  TaskPriority,
  TaskCategory,
  CreateTask,
  UpdateTask
} from '../../models/task';

import { TaskService } from '../../services/task';
import { CreateTaskDialog } from '../../components/create-task-dialog/create-task-dialog';
import { EditTaskDialog } from '../../components/edit-task-dialog/edit-task-dialog';
import { ConfirmDialog } from '../../components/confirm-dialog/confirm-dialog';

type TaskFilter = TaskStatus | 'all';
type TaskSort = 'newest' | 'oldest' | 'title';
type TaskView = 'kanban' | 'list';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    FormsModule,
    DatePipe,
    DragDropModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatCheckboxModule,
    MatButtonToggleModule,
    MatSelectModule
  ],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss'
})
export class Tasks implements OnInit {

  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly tasks = signal<Task[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly creating = signal(false);

  readonly deletingTaskId = signal<number | null>(null);
  readonly movingTaskId = signal<number | null>(null);

  readonly viewMode = signal<TaskView>('kanban');

  readonly filter = signal<TaskFilter>('all');
  readonly searchTerm = signal('');
  readonly sortBy = signal<TaskSort>('newest');

  // INDICADORES

  readonly pendingCount = computed(() =>
    this.tasks().filter(task => task.status === 'pending').length
  );

  readonly inProgressCount = computed(() =>
    this.tasks().filter(task => task.status === 'in_progress').length
  );

  readonly completedCount = computed(() =>
    this.tasks().filter(task => task.status === 'completed').length
  );

  // BÚSQUEDA Y ORDENACIÓN COMPARTIDAS

  readonly visibleTasks = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const sort = this.sortBy();

    const result = this.tasks().filter(task =>
      task.title.toLowerCase().includes(search) ||
      (task.description ?? '').toLowerCase().includes(search)
    );

    return result.sort((a, b) => {
      if (sort === 'title') {
        return a.title.localeCompare(b.title, 'es');
      }

      const dateA = new Date(a.createdAt).getTime();
      const dateB = new Date(b.createdAt).getTime();

      return sort === 'oldest'
        ? dateA - dateB
        : dateB - dateA;
    });
  });

  // VISTA LISTA

  readonly filteredTasks = computed(() => {
    const currentFilter = this.filter();

    if (currentFilter === 'all') {
      return this.visibleTasks();
    }

    return this.visibleTasks().filter(
      task => task.status === currentFilter
    );
  });

  // VISTA KANBAN

  readonly pendingTasks = computed(() =>
    this.visibleTasks().filter(task => task.status === 'pending')
  );

  readonly inProgressTasks = computed(() =>
    this.visibleTasks().filter(task => task.status === 'in_progress')
  );

  readonly completedTasks = computed(() =>
    this.visibleTasks().filter(task => task.status === 'completed')
  );

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.error.set('');

    this.taskService.getTasks()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: tasks => {
          this.tasks.set(tasks);
          this.loading.set(false);
        },
        error: err => {
          console.error('Error loading tasks:', err);
          this.error.set('No se pudieron cargar las tareas.');
          this.loading.set(false);
        }
      });
  }

  // CREAR EN DIÁLOGO DE ANGULAR MATERIAL

  openCreateDialog(): void {
    if (this.creating() || this.dialog.getDialogById('task-create-dialog')) {
      return;
    }

    const dialogRef = this.dialog.open<
      CreateTaskDialog,
      void,
      CreateTask
    >(CreateTaskDialog, {
      id: 'task-create-dialog',
      width: '560px',
      maxWidth: '95vw'
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.creating.set(true);
        this.error.set('');

        this.taskService.createTask(result)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.creating.set(false);
              this.loadTasks();
              this.showNotification('Tarea creada correctamente');
            },
            error: err => {
              console.error('Error creating task:', err);
              this.error.set('No se pudo crear la tarea.');
              this.creating.set(false);
              this.showNotification('No se pudo crear la tarea');
            }
          });
      });
  }

  // CAMBIAR ESTADO

  changeStatus(
    task: Task,
    newStatus: TaskStatus
  ): void {
    if (
      task.status === newStatus ||
      this.movingTaskId() !== null
    ) {
      return;
    }

    const previousStatus = task.status;
    this.error.set('');
    this.movingTaskId.set(task.id);

    // Actualización optimista.
    this.updateLocalStatus(task.id, newStatus);

    this.taskService.updateTaskStatus(task.id, {
      status: newStatus
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.movingTaskId.set(null);
          // Refrescar updatedAt con el valor confirmado por la API.
          this.loadTasks();

          this.showNotification(
            `Tarea movida a ${this.statusLabel(newStatus)}`
          );
        },
        error: err => {
          console.error('Error updating task status:', err);

          // Restaurar el estado anterior.
          this.updateLocalStatus(task.id, previousStatus);
          this.movingTaskId.set(null);

          this.error.set(
            'No se pudo guardar el cambio de estado.'
          );

          this.showNotification(
            'No se pudo mover la tarea'
          );
        }
      });
  }

  private updateLocalStatus(
    taskId: number,
    status: TaskStatus
  ): void {
    this.tasks.update(tasks =>
      tasks.map(task =>
        task.id === taskId
          ? {
              ...task,
              status,
              isCompleted: status === 'completed'
            }
          : task
      )
    );
  }

  // DRAG & DROP

  dropTask(
    event: CdkDragDrop<Task[]>,
    targetStatus: TaskStatus
  ): void {
    const task = event.item.data as Task;

    if (!task) {
      return;
    }

    this.changeStatus(task, targetStatus);
  }

  // CHECKBOX EN VISTA LISTA

  toggleTask(task: Task): void {
    const newStatus: TaskStatus =
      task.status === 'completed'
        ? 'pending'
        : 'completed';

    this.changeStatus(task, newStatus);
  }

  // EDITAR

  editTask(task: Task): void {
    const dialogRef = this.dialog.open<
      EditTaskDialog,
      Task,
      UpdateTask
    >(EditTaskDialog, {
      width: '560px',
      maxWidth: '95vw',
      data: task
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) {
          return;
        }

        this.error.set('');

        this.taskService.updateTask(task.id, result)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.loadTasks();
              this.showNotification(
                'Tarea actualizada correctamente'
              );
            },
            error: err => {
              console.error('Error updating task:', err);
              this.error.set(
                'No se pudo actualizar la tarea.'
              );
            }
          });
      });
  }

  // ELIMINAR

  deleteTask(task: Task): void {
    const dialogRef = this.dialog.open<
      ConfirmDialog,
      { title: string },
      boolean
    >(ConfirmDialog, {
      width: '420px',
      maxWidth: '95vw',
      data: { title: task.title }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) {
          return;
        }

        this.deletingTaskId.set(task.id);
        this.error.set('');

        this.taskService.deleteTask(task.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.deletingTaskId.set(null);
              this.loadTasks();

              this.showNotification(
                'Tarea eliminada correctamente'
              );
            },
            error: err => {
              console.error('Error deleting task:', err);
              this.error.set(
                'No se pudo eliminar la tarea.'
              );
              this.deletingTaskId.set(null);
            }
          });
      });
  }

  statusLabel(status: TaskStatus): string {
    switch (status) {
      case 'pending':
        return 'Pendiente';
      case 'in_progress':
        return 'En progreso';
      case 'completed':
        return 'Completada';
    }
  }

  priorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case 'low': return 'Baja';
      case 'medium': return 'Media';
      case 'high': return 'Alta';
      case 'urgent': return 'Urgente';
    }
  }

  categoryLabel(category: TaskCategory): string {
    switch (category) {
      case 'frontend': return 'Frontend';
      case 'backend': return 'Backend';
      case 'database': return 'Base de datos';
      case 'documentation': return 'Documentación';
      case 'other': return 'Otros';
    }
  }

  dueDateLabel(dueDate: string): string {
    const [year, month, day] = dueDate.split('-');
    return `${day}/${month}/${year}`;
  }

  isOverdue(task: Task): boolean {
    if (!task.dueDate || task.status === 'completed') {
      return false;
    }

    const today = new Date();
    const localToday = [
      today.getFullYear(),
      String(today.getMonth() + 1).padStart(2, '0'),
      String(today.getDate()).padStart(2, '0')
    ].join('-');

    return task.dueDate < localToday;
  }

  private showNotification(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom'
    });
  }
}
