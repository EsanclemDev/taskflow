
import { Task } from '../models/task';

export interface TaskMetrics {
  total: number;
  completed: number;
  pending: number;
  completionRate: number;
}

export interface MonthlyTaskCount {
  key: string;
  label: string;
  count: number;
}

export function calculateTaskMetrics(tasks: Task[]): TaskMetrics {
  const total = tasks.length;
  const completed = tasks.filter(task => task.isCompleted).length;
  const pending = total - completed;

  return {
    total,
    completed,
    pending,
    completionRate:
      total === 0 ? 0 : Math.round((completed / total) * 100)
  };
}

export function getRecentTasks(tasks: Task[], limit = 5): Task[] {
  return [...tasks]
    .sort(
      (a, b) =>
        new Date(b.createdAt).getTime() -
        new Date(a.createdAt).getTime()
    )
    .slice(0, limit);
}

export function getMonthlyCreatedTasks(
  tasks: Task[],
  months = 6,
  now = new Date()
): MonthlyTaskCount[] {
  const result: MonthlyTaskCount[] = [];
  const formatter = new Intl.DateTimeFormat('es-ES', {
    month: 'short'
  });

  for (let offset = months - 1; offset >= 0; offset--) {
    const date = new Date(
      now.getFullYear(),
      now.getMonth() - offset,
      1
    );

    const year = date.getFullYear();
    const month = date.getMonth();

    const count = tasks.filter(task => {
      const createdAt = new Date(task.createdAt);

      return (
        !Number.isNaN(createdAt.getTime()) &&
        createdAt.getFullYear() === year &&
        createdAt.getMonth() === month
      );
    }).length;

    result.push({
      key: `${year}-${String(month + 1).padStart(2, '0')}`,
      label: `${formatter.format(date)} ${year}`,
      count
    });
  }

  return result;
}
