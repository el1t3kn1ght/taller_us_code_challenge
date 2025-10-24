// Task model matching backend
export interface TaskItem {
  id: number;
  title: string;
  description: string;
  createdAt: string;
  isCompleted: boolean;
}

// Task summary from AI
export interface TaskSummary {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  aiSummary: string;
  generatedAt: string;
}

// Form data for new tasks
export interface NewTaskForm {
  title: string;
  description: string;
}

// Connection status type
export type ConnectionStatus = 
  | 'Connected' 
  | 'Disconnected' 
  | 'Reconnecting' 
  | 'Connection Failed';