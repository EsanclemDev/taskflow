
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Task } from '../../models/task';
import { TaskService } from '../../services/task';
import {
  calculateTaskMetrics,
  getMonthlyCreatedTasks
} from '../../utils/task-metrics';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [RouterLink, MatIconModule],
  templateUrl: './statistics.html',
  styleUrl: './statistics.scss'
})
export class Statistics implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);

  readonly tasks = signal<Task[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly metrics = computed(() =>
    calculateTaskMetrics(this.tasks())
  );

  readonly monthlyTasks = computed(() =>
    getMonthlyCreatedTasks(this.tasks())
  );

  readonly maxMonthlyTasks = computed(() =>
    Math.max(1, ...this.monthlyTasks().map(item => item.count))
  );

  readonly donutBackground = computed(() => {
    const progress = this.metrics().completionRate;

    return `conic-gradient(
      #2563eb 0% ${progress}%,
      #e2e8f0 ${progress}% 100%
    )`;
  });

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
