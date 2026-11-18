
export type TaskStatus =
  | 'pending'
  | 'in_progress'
  | 'completed';

export interface Task {
  id: number;
  title: string;
  description: string | null;
  isCompleted: boolean;
  status: TaskStatus;
  createdAt: string;
}

export interface CreateTask {
  title: string;
  description: string | null;
}

export interface UpdateTask {
  title: string;
  description: string | null;
  isCompleted?: boolean;
  status?: TaskStatus;
}

export interface UpdateTaskStatus {
  status: TaskStatus;
}
