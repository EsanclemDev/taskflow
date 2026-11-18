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

  // Distribución real de los tres estados del Kanban.
  readonly donutBackground = computed(() => {
    const { total, inProgress, completed } = this.metrics();

    if (total === 0) {
      return 'conic-gradient(#e2e8f0 0% 100%)';
    }

    const completedEnd = (completed / total) * 100;
    const progressEnd = ((completed + inProgress) / total) * 100;

    return `conic-gradient(
      #16a34a 0% ${completedEnd}%,
      #f59e0b ${completedEnd}% ${progressEnd}%,
      #cbd5e1 ${progressEnd}% 100%
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
