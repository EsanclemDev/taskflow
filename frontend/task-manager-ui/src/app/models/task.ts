export type TaskStatus = 'pending' | 'in_progress' | 'completed';
export type TaskPriority = 'low' | 'medium' | 'high' | 'urgent';
export type TaskCategory = 'frontend' | 'backend' | 'database' | 'documentation' | 'other';

export interface Task {
  id: number;
  title: string;
  description: string | null;
  isCompleted: boolean;
  status: TaskStatus;
  priority: TaskPriority;
  category: TaskCategory;
  dueDate: string | null; // YYYY-MM-DD (sin zona horaria)
  createdAt: string;
  updatedAt: string;
  userId: string | null;
  createdByUserId: string;
  createdByUserName: string | null;
  assignedToUserId: string;
  assignedToUserName: string | null;
}

export interface CreateTask {
  title: string;
  description: string | null;
  priority: TaskPriority;
  category: TaskCategory;
  dueDate: string | null;
}

export interface UpdateTask {
  title: string;
  description: string | null;
  isCompleted?: boolean;
  status?: TaskStatus;
  priority?: TaskPriority;
  category?: TaskCategory;
  dueDate?: string | null;
  clearDueDate?: boolean;
}

export interface UpdateTaskStatus {
  status: TaskStatus;
}
