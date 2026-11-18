
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Task } from '../../models/task';
import { TaskService } from '../../services/task';
import {
  calculateTaskMetrics,
  getRecentTasks
} from '../../utils/task-metrics';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, MatIconModule, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);

  readonly tasks = signal<Task[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly metrics = computed(() =>
    calculateTaskMetrics(this.tasks())
  );

  readonly recentTasks = computed(() =>
    getRecentTasks(this.tasks())
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
        error: () => {
          this.error.set('No se pudieron cargar las estadísticas.');
          this.loading.set(false);
        }
      });
  }
}
