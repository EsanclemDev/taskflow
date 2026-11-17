
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { TaskService } from '../../services/task';
import { Task, UpdateTask } from '../../models/task';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { EditTaskDialog } from '../../components/edit-task-dialog/edit-task-dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ConfirmDialog } from '../../components/confirm-dialog/confirm-dialog';
import { MatSelectModule } from '@angular/material/select';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    FormsModule,
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

  private taskService = inject(TaskService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private authService = inject(AuthService);
  private router = inject(Router);

  loggingOut = signal(false);
  tasks = signal<Task[]>([]);
  loading = signal(false);
  error = signal('');
  filter = signal<'all' | 'pending' | 'completed'>('all');
  searchTerm = signal('');
  sortBy = signal<'newest' | 'oldest' | 'title'>('newest');
  creating = signal(false);

  newTask = {
    title: '',
    description: ''
  };

  pendingCount = computed(
    () => this.tasks().filter(task => !task.isCompleted).length
  );

  completedCount = computed(
    () => this.tasks().filter(task => task.isCompleted).length
  );


  filteredTasks = computed(() => {
    const currentFilter = this.filter();
    const search = this.searchTerm().trim().toLowerCase();
    const sort = this.sortBy();

    const filtered = this.tasks().filter(task => {
      const matchesStatus =
        currentFilter === 'all' ||
        (currentFilter === 'pending' && !task.isCompleted) ||
        (currentFilter === 'completed' && task.isCompleted);

      const matchesSearch =
        task.title.toLowerCase().includes(search) ||
        (task.description ?? '').toLowerCase().includes(search);

      return matchesStatus && matchesSearch;
    });

    return filtered.sort((a, b) => {
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


  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.error.set('');

    this.taskService.getTasks().subscribe({
      next: (tasks) => {
        console.log('Tareas recibidas:', tasks);
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading tasks:', err);
        this.error.set('No se pudieron cargar las tareas.');
        this.loading.set(false);
      }
    });
  }

  createTask(form: NgForm): void {
    const title = this.newTask.title.trim();

    if (title.length < 3 || title.length > 100) {
      this.error.set('El título debe tener entre 3 y 100 caracteres.');
      return;
    }

    this.creating.set(true);
    this.error.set('');

    this.taskService.createTask({
      title,
      description: this.newTask.description.trim() || null
    }).subscribe({
      next: () => {
        form.resetForm({
          title: '',
          description: ''
        });

        this.creating.set(false);
        this.loadTasks();
        this.showNotification('Tarea creada correctamente');
      },
      error: (err) => {
        console.error('Error creating task:', err);
        this.error.set('No se pudo crear la tarea.');
        this.creating.set(false);
      }
    });
  }

  toggleTask(task: Task): void {
    this.error.set('');

    this.taskService.updateTask(task.id, {
      title: task.title,
      description: task.description,
      isCompleted: !task.isCompleted
    }).subscribe({
      next: () => {
        this.loadTasks();
        this.showNotification(
          task.isCompleted
            ? 'Tarea marcada como pendiente'
            : 'Tarea completada'
        );
      },
      error: (err) => {
        console.error('Error updating task:', err);
        this.error.set('No se pudo actualizar la tarea.');
      }
    });
  }

  deletingTaskId = signal<number | null>(null);


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

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        return;
      }

      this.deletingTaskId.set(task.id);
      this.error.set('');

      this.taskService.deleteTask(task.id).subscribe({
        next: () => {
          this.deletingTaskId.set(null);
          this.loadTasks();
          this.showNotification('Tarea eliminada correctamente');
        },
        error: err => {
          console.error('Error deleting task:', err);
          this.error.set('No se pudo eliminar la tarea.');
          this.deletingTaskId.set(null);
        }
      });
    });
  }


  editTask(task: Task): void {
    const dialogRef = this.dialog.open<
      EditTaskDialog,
      Task,
      UpdateTask
    >(EditTaskDialog, {
      width: '480px',
      maxWidth: '95vw',
      data: task
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        return;
      }

      this.taskService.updateTask(task.id, result).subscribe({
        next: () => {
          this.loadTasks();
          this.showNotification('Tarea actualizada correctamente');
        },
        error: err => {
          console.error('Error updating task:', err);
          this.error.set('No se pudo actualizar la tarea.');
        }
      });
    });
  }

  private showNotification(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom'
    });
  }

  logout(): void {
    if (this.loggingOut()) {
      return;
    }

    this.loggingOut.set(true);

    this.authService.logout().subscribe({
      next: () => {
        this.loggingOut.set(false);
        void this.router.navigateByUrl('/login');
      },
      error: (err) => {
        console.error('Error al cerrar sesión:', err);
        this.loggingOut.set(false);
        this.showNotification(
          'No se pudo confirmar el cierre de sesión en el servidor.'
        );
        void this.router.navigateByUrl('/login');
      }
    });
  }

}
