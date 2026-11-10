export interface Task {
  id: number;
  title: string;
  description: string | null;
  isCompleted: boolean;
  createdAt: string;
}

export interface CreateTask {
  title: string;
  description: string | null;
}

export interface UpdateTask {
  title: string;
  description: string | null;
  isCompleted: boolean;
}