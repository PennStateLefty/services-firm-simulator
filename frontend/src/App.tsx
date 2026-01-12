import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import './App.css';

// Pages will be created as we implement user stories
const Dashboard = () => <div>Dashboard - Coming Soon</div>;
const Employees = () => <div>Employees - Coming Soon</div>;
const Onboarding = () => <div>Onboarding - Coming Soon</div>;
const Performance = () => <div>Performance - Coming Soon</div>;
const Merit = () => <div>Merit - Coming Soon</div>;
const Offboarding = () => <div>Offboarding - Coming Soon</div>;

function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <nav className="navbar">
          <h1>HR System Simulator</h1>
          <div className="nav-links">
            <a href="/">Dashboard</a>
            <a href="/employees">Employees</a>
            <a href="/onboarding">Onboarding</a>
            <a href="/performance">Performance</a>
            <a href="/merit">Merit</a>
            <a href="/offboarding">Offboarding</a>
          </div>
        </nav>
        
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/employees" element={<Employees />} />
            <Route path="/onboarding" element={<Onboarding />} />
            <Route path="/performance" element={<Performance />} />
            <Route path="/merit" element={<Merit />} />
            <Route path="/offboarding" element={<Offboarding />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
