import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import axios, { AxiosError } from 'axios';
import './App.css';
import { TaskItem, TaskSummary, NewTaskForm, ConnectionStatus } from './types';
import { useToast } from './hooks/useToast';
import Toast from './Components/Toast'

const API_URL = 'https://localhost:7131/api';
const HUB_URL = 'https://localhost:7131/taskHub';

function App() {
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [newTask, setNewTask] = useState<NewTaskForm>({ title: '', description: '' });
  const [summary, setSummary] = useState<TaskSummary | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [connectionStatus, setConnectionStatus] = useState<ConnectionStatus>('Disconnected');
  
  const { toasts, showToast, removeToast } = useToast();
  
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const isConnectionStarted = useRef(false);
  const currentUserActionRef = useRef<string | null>(null);

// Initialize and start SignalR connection
useEffect(() => {
  // Build connection with better error handling
  const newConnection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      transport: signalR.HttpTransportType.WebSockets | 
                 signalR.HttpTransportType.ServerSentEvents | 
                 signalR.HttpTransportType.LongPolling,
      skipNegotiation: false,
      withCredentials: false
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: () => {
        return 3000;
      }
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connectionRef.current = newConnection;

  // Register event handlers BEFORE starting
// Register event handlers BEFORE starting
newConnection.on('TaskAdded', (task: TaskItem) => {
  console.log('📥 Task added via SignalR:', task);
  console.log('📥 Current user action ref:', currentUserActionRef.current);
  
  // Only show toast if not current user's action
  if (currentUserActionRef.current !== `add-${task.id}`) {
    console.log('✅ Showing toast for task added by another user');
    showToast(`✨ New task added by another user: "${task.title}"`, 'info');
  } else {
    console.log('🔕 Ignoring own action');
  }
});

newConnection.on('TaskUpdated', (updatedTask: TaskItem) => {
  console.log('📝 Task updated via SignalR:', updatedTask);
  
  // Only show toast if not current user's action
  if (currentUserActionRef.current !== `update-${updatedTask.id}`) {
    const status = updatedTask.isCompleted ? 'completed' : 'reopened';
    console.log('✅ Showing toast for task updated by another user');
    showToast(
      `${updatedTask.isCompleted ? '✅' : '🔄'} Task ${status} by another user: "${updatedTask.title}"`, 
      'info'
    );
  } else {
    console.log('🔕 Ignoring own action');
  }
});

newConnection.on('TaskDeleted', (taskId: number) => {
  console.log('🗑️ Task deleted via SignalR:', taskId);
  
  // Only show toast if not current user's action
  if (currentUserActionRef.current !== `delete-${taskId}`) {
    // Find task name for the toast (from current state)
    const deletedTask = tasks.find(t => t.id === taskId);
    if (deletedTask) {
      console.log('✅ Showing toast for task deleted by another user');
      showToast(`🗑️ Task deleted by another user: "${deletedTask.title}"`, 'info');
    }
  } else {
    console.log('🔕 Ignoring own action');
  }
});

  // Connection lifecycle handlers
  newConnection.onreconnecting((error) => {
    console.log('⏳ SignalR reconnecting...', error);
    setConnectionStatus('Reconnecting');
    showToast('⏳ Reconnecting to server...', 'warning');
  });

  newConnection.onreconnected((connectionId) => {
    console.log('✅ SignalR reconnected. Connection ID:', connectionId);
    setConnectionStatus('Connected');
    showToast('✅ Reconnected successfully!', 'success');
  });

  newConnection.onclose((error) => {
    console.log('❌ SignalR connection closed', error);
    setConnectionStatus('Disconnected');
    
    if (error) {
      showToast('⚠️ Connection to server lost', 'warning');
    }
  });

  // Start connection
  const startConnection = async () => {
    try {
      console.log('🔌 Attempting to connect to SignalR...');
      setConnectionStatus('Reconnecting');
      
      await newConnection.start();
      
      console.log('✅ SignalR connected successfully!');
      setConnectionStatus('Connected');
      showToast('🔌 Connected to real-time updates!', 'success');
      
    } catch (err: any) {
      console.error('❌ SignalR connection failed:', err.message);
      setConnectionStatus('Connection Failed');
      showToast('❌ Failed to connect to server. Real-time updates disabled.', 'warning');
    }
  };

  startConnection();

  // Cleanup function
  return () => {
    console.log('🔌 Cleaning up SignalR connection...');
    
    if (newConnection.state === signalR.HubConnectionState.Connected) {
      newConnection.stop()
        .then(() => {
          console.log('✅ SignalR connection stopped cleanly');
        })
        .catch((err) => {
          console.error('Error stopping connection:', err);
        });
    }
  };
}, []); // ⚠️ EMPTY dependency array - only run once

  // Fetch all tasks on mount
  useEffect(() => {
    fetchTasks();
  }, []);

  const fetchTasks = async (): Promise<void> => {
    try {
      console.log('🔄 Fetching tasks from:', `${API_URL}/tasks`);
      const response = await axios.get<TaskItem[]>(`${API_URL}/tasks`);
      setTasks(response.data);
      console.log('✅ Fetched tasks:', response.data.length);
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error('❌ Error fetching tasks:', axiosError.message);
      showToast('❌ Failed to load tasks', 'error');
    }
  };

  const addTask = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    
    if (!newTask.title.trim()) {
      showToast('⚠️ Please enter a task title', 'warning');
      return;
    }

    try {
      const taskToAdd: Omit<TaskItem, 'id' | 'createdAt'> = {
        title: newTask.title,
        description: newTask.description,
        isCompleted: false
      };

      console.log('➕ Adding task:', taskToAdd);
      const response = await axios.post<TaskItem>(`${API_URL}/tasks`, taskToAdd);
      
      // Mark this as current user's action to prevent duplicate updates from SignalR
      currentUserActionRef.current = `add-${response.data.id}`;
      setTimeout(() => { currentUserActionRef.current = null; }, 2000); // Increased to 2 seconds
      
      // ✅ Immediately update local state with the response
      setTasks(prev => [response.data, ...prev]);
      setNewTask({ title: '', description: '' });
      showToast(`✨ Task added: "${response.data.title}"`, 'success');
      console.log('✅ Task added successfully');
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error('❌ Error adding task:', axiosError.message);
      showToast('❌ Failed to add task', 'error');
    }
  };

  const toggleTaskComplete = async (task: TaskItem): Promise<void> => {
    try {
      const updatedTask: TaskItem = {
        ...task,
        isCompleted: !task.isCompleted
      };

      console.log('🔄 Toggling task:', updatedTask);
      
      // Mark this as current user's action
      currentUserActionRef.current = `update-${task.id}`;
      setTimeout(() => { currentUserActionRef.current = null; }, 1000);
      
      const response = await axios.put<TaskItem>(`${API_URL}/tasks/${task.id}`, updatedTask);
      
      // ✅ Immediately update local state
      setTasks(prev => prev.map(t => 
        t.id === response.data.id ? response.data : t
      ));
      
      const status = response.data.isCompleted ? 'completed' : 'reopened';
      showToast(
        `${response.data.isCompleted ? '✅' : '🔄'} Task ${status}: "${response.data.title}"`, 
        response.data.isCompleted ? 'success' : 'info'
      );
      console.log('✅ Task toggled');
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error('❌ Error updating task:', axiosError.message);
      showToast('❌ Failed to update task', 'error');
    }
  };

  const deleteTask = async (taskId: number): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this task?')) {
      return;
    }

    // Get task title before deleting for the toast
    const taskToDelete = tasks.find(t => t.id === taskId);

    try {
      console.log('🗑️ Deleting task:', taskId);
      
      // Mark this as current user's action
      currentUserActionRef.current = `delete-${taskId}`;
      setTimeout(() => { currentUserActionRef.current = null; }, 1000);
      
      await axios.delete(`${API_URL}/tasks/${taskId}`);
      
      // ✅ Immediately update local state
      setTasks(prev => prev.filter(t => t.id !== taskId));
      
      if (taskToDelete) {
        showToast(`🗑️ Task deleted: "${taskToDelete.title}"`, 'warning');
      }
      console.log('✅ Task deleted');
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error('❌ Error deleting task:', axiosError.message);
      showToast('❌ Failed to delete task', 'error');
    }
  };

  const generateSummary = async (): Promise<void> => {
    if (tasks.length === 0) {
      showToast('⚠️ Add some tasks first!', 'warning');
      return;
    }

    setLoading(true);
    showToast('🤖 Generating AI summary...', 'info');
    
    try {
      console.log('🤖 Generating AI summary...');
      const response = await axios.post<TaskSummary>(`${API_URL}/tasks/summary`);
      setSummary(response.data);
      console.log('✅ Summary generated');
      showToast('✨ AI summary generated successfully!', 'success');
    } catch (error) {
      const axiosError = error as AxiosError;
      console.error('❌ Error generating summary:', axiosError.message);
      showToast('❌ Failed to generate summary', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ): void => {
    const { name, value } = e.target;
    setNewTask(prev => ({ ...prev, [name]: value }));
  };

  return (
    <div className="App">
      {/* Toast Container */}
      <div className="toast-container">
        {toasts.map(toast => (
          <Toast
            key={toast.id}
            message={toast.message}
            type={toast.type}
            onClose={() => removeToast(toast.id)}
          />
        ))}
      </div>

      <header className="App-header">
        <h1>📋 Real-Time Task Manager</h1>
        <p>with AI-Powered Summarization</p>
        <div className={`connection-status ${connectionStatus.toLowerCase().replace(' ', '-')}`}>
          <span className="status-dot"></span>
          {connectionStatus}
        </div>
      </header>

      <div className="container">
        {/* Add Task Form */}
        <div className="card">
          <h2>➕ Add New Task</h2>
          <form onSubmit={addTask}>
            <input
              type="text"
              name="title"
              placeholder="Task Title *"
              value={newTask.title}
              onChange={handleInputChange}
              required
            />
            <textarea
              name="description"
              placeholder="Task Description (optional)"
              value={newTask.description}
              onChange={handleInputChange}
              rows={3}
            />
            <button type="submit" className="btn-primary">
              ➕ Add Task
            </button>
          </form>
        </div>

        {/* AI Summary Section */}
        <div className="card">
          <h2>🤖 AI Summary</h2>
          <button 
            onClick={generateSummary} 
            className="btn-secondary"
            disabled={loading || tasks.length === 0}
          >
            {loading ? '⏳ Generating...' : '✨ Generate AI Summary'}
          </button>
          
          {summary && (
            <div className="summary-box">
              <div className="stats">
                <div className="stat">
                  <span className="stat-label">Total</span>
                  <span className="stat-value">{summary.totalTasks}</span>
                </div>
                <div className="stat">
                  <span className="stat-label">Completed</span>
                  <span className="stat-value completed">{summary.completedTasks}</span>
                </div>
                <div className="stat">
                  <span className="stat-label">Pending</span>
                  <span className="stat-value pending">{summary.pendingTasks}</span>
                </div>
              </div>
              <div className="ai-summary">
                <h3>🎯 AI Insights:</h3>
                <p>{summary.aiSummary}</p>
                <small>Generated: {new Date(summary.generatedAt).toLocaleString()}</small>
              </div>
            </div>
          )}
        </div>

        {/* Task List */}
        <div className="card">
          <h2>📝 Tasks ({tasks.length})</h2>
          {tasks.length === 0 ? (
            <div className="empty-state">
              <div className="empty-icon">📭</div>
              <p>No tasks yet. Add one above!</p>
            </div>
          ) : (
            <div className="task-list">
              {tasks.map(task => (
                <div key={task.id} className={`task-item ${task.isCompleted ? 'completed' : ''}`}>
                  <div className="task-checkbox">
                    <input
                      type="checkbox"
                      checked={task.isCompleted}
                      onChange={() => toggleTaskComplete(task)}
                      id={`task-${task.id}`}
                    />
                    <label htmlFor={`task-${task.id}`}></label>
                  </div>
                  <div className="task-content">
                    <h3>{task.title}</h3>
                    {task.description && <p>{task.description}</p>}
                    <small>
                      📅 {new Date(task.createdAt).toLocaleString()}
                    </small>
                  </div>
                  <button 
                    className="btn-delete"
                    onClick={() => deleteTask(task.id)}
                    title="Delete task"
                  >
                    🗑️
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <footer className="app-footer">
        <p>Built with ❤️ using ASP.NET Core, React + TypeScript, SignalR & OpenAI</p>
      </footer>
    </div>
  );
}

export default App;