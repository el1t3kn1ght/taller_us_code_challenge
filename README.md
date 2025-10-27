# 📋 Real-Time Task Manager

A full-stack task management application with real-time updates using SignalR and AI-powered task summarization.

## 🚀 Features

- ✨ Real-time task updates across multiple users/tabs
- 🤖 AI-powered task summarization using OpenAI
- ✅ Create, update, complete, and delete tasks
- 🔔 Toast notifications for all actions
- 📊 Task statistics and insights
- 🎨 Modern, responsive UI built with React + TypeScript

## 🛠️ Tech Stack

**Backend:**
- ASP.NET Core (Minimal APIs)
- SignalR for real-time communication
- Entity Framework Core with SQLite
- OpenAI API integration

**Frontend:**
- React + TypeScript
- SignalR Client
- Axios for HTTP requests
- CSS3 for styling

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18 or higher)
- [OpenAI API Key](https://platform.openai.com/api-keys)

## ⚙️ Setup Instructions

### Backend Setup

1. **Navigate to the backend directory:**
   ```bash
   cd backend
   ```

2. **Install dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure OpenAI API Key:**
   - Add your OpenAI API key to `appsettings.json`:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-api-key-here"
     }
   }
   ```

4. **⚠️ IMPORTANT: Apply Database Migration:**
   
   Before running the backend, you **MUST** create and apply the database migration:
   
   ```bash
   # Create a migration (if not already created)
   dotnet ef migrations add InitialCreate
   
   # Apply the migration to create the database
   dotnet ef database update
   ```
   
   **OR** if using Package Manager Console in Visual Studio:
   ```powershell
   Update-Database
   ```
   
   > ⚠️ **This step is required!** The application will not work without the database.

5. **Run the backend:**
   ```bash
   dotnet run
   ```
   
   The API will be available at:
   - HTTPS: `https://localhost:7131`
   - HTTP: `http://localhost:5000`
   - Swagger: `https://localhost:7131/swagger`

### Frontend Setup

1. **Navigate to the frontend directory:**
   ```bash
   cd frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm start
   ```
   
   The app will open at `http://localhost:3000`

## 🎯 Usage

1. **Add Tasks:** Enter a task title and optional description, then click "Add Task"
2. **Complete Tasks:** Click the checkbox to mark tasks as complete/incomplete
3. **Delete Tasks:** Click the trash icon to remove tasks
4. **AI Summary:** Click "Generate AI Summary" to get AI-powered insights about your tasks
5. **Real-Time Updates:** Open the app in multiple tabs to see real-time synchronization

## 📁 Project Structure

```
├── backend/
│   ├── Context/          # Database context
│   ├── Entities/         # Data models
│   ├── Dtos/            # Data transfer objects
│   ├── Hub/             # SignalR hub
│   ├── SummarizationServices/  # OpenAI integration
│   └── Program.cs       # Application entry point
│
└── frontend/
    ├── src/
    │   ├── Components/   # React components
    │   ├── hooks/       # Custom React hooks
    │   ├── types/       # TypeScript type definitions
    │   └── App.tsx      # Main application component
    └── public/
```

## 🔧 Configuration

### Backend Ports
Configure in `Properties/launchSettings.json`:
```json
{
  "applicationUrl": "https://localhost:7131;http://localhost:5000"
}
```

### Frontend API URLs
Configure in `src/App.tsx`:
```typescript
const API_URL = 'https://localhost:7131/api';
const HUB_URL = 'https://localhost:7131/taskHub';
```

## 🐛 Troubleshooting

### Database Issues
If you encounter database errors:
```bash
# Delete the database and recreate
rm tasks.db
dotnet ef database update
```

### SignalR Connection Issues
- Ensure the backend is running before starting the frontend
- Check that CORS is properly configured
- Verify the ports match between frontend and backend

### Port Already in Use
```bash
# Windows - Kill process on port 7131
netstat -ano | findstr :7131
taskkill /PID <process_id> /F

# Mac/Linux
lsof -ti:7131 | xargs kill -9
```

## 📝 API Endpoints

- `GET /api/tasks` - Get all tasks
- `GET /api/tasks/{id}` - Get task by ID
- `POST /api/tasks` - Create new task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `POST /api/tasks/summary` - Generate AI summary

## 🔌 SignalR Events

- `TaskAdded` - Fired when a new task is created
- `TaskUpdated` - Fired when a task is updated
- `TaskDeleted` - Fired when a task is deleted

## 📄 License

This project is licensed under the MIT License.

## 🙏 Acknowledgments

- Built with ❤️ using ASP.NET Core, React, TypeScript, SignalR & OpenAI
- Icons and emojis for enhanced user experience

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

---

**Happy Task Managing! 📋✨**
