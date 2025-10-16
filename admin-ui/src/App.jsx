import { Routes, Route, Navigate, useNavigate } from "react-router-dom";
import Login from "./Login";
import Cards from "./Cards";

// ป้องกันหน้า admin ถ้ายังไม่มี token
function PrivateRoute({ children }) {
  const token = localStorage.getItem("token");
  return token ? children : <Navigate to="/" replace />;
}

function CardsPage() {
  const navigate = useNavigate();
  const onLogout = () => {
    localStorage.removeItem("token");
    navigate("/", { replace: true });
  };
  return <Cards onLogout={onLogout} />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Login />} />
      <Route
        path="/cards"
        element={
          <PrivateRoute>
            <CardsPage />
          </PrivateRoute>
        }
      />
      {/* กันเคสเส้นทางอื่น ๆ */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
