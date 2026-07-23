import React, { useState, useEffect } from 'react';
import { LuSearch, LuX } from "react-icons/lu";
import api from '../services/api';
import '../App.css';

const Lotes: React.FC = () => {
    const [lotes, setLotes] = useState<any[]>([]);
    const [busqueda, setBusqueda] = useState('');
    const [error, setError] = useState<string | null>(null);

    const cargarLotes = async () => {
        try {
            const res = await api.get('/lotes/Listar');
            if (res.data.success) {
                setLotes(res.data.data);
            }
        } catch (err) {
            setError("Error al cargar los lotes.");
        }
    };

    useEffect(() => {
        cargarLotes();
    }, []);

    // Filtro corregido: usando 'l' (cada objeto) para acceder a sus propiedades
    const lotesFiltrados = lotes.filter(l => {
        const termino = busqueda.toLowerCase();
        return (
            (l.nombreProducto?.toLowerCase() || '').includes(termino) ||
            (l.codigoLote?.toLowerCase() || '').includes(termino)
        );
    });

    return (
        <div className="clientes-wrapper">
            <div className="clientes-header">
                <h1 className="clientes-title">Gestión de Lotes</h1>
            </div>

            {error && <div className="error-message" style={{ color: 'red' }}>{error}</div>}

            <div className="clientes-controls">
                <div className="control-box flex-grow">
                    <span className="control-label">Búsqueda Inteligente</span>
                    <div className="search-input-group">
                        <input
                            type="text"
                            placeholder="Buscar por Producto o Código de Lote..."
                            value={busqueda}
                            onChange={(e) => setBusqueda(e.target.value)}
                        />
                        <button className="btn" type="button"><LuSearch /></button>
                    </div>
                </div>
            </div>

            <div className="table-container">
                <table className="clientes-table">
                    <thead>
                        <tr>
                            <th>Producto</th>
                            <th>Código Lote</th>
                            <th>Vencimiento</th>
                            <th>Stock Restante</th>
                        </tr>
                    </thead>
                    <tbody>
                        {/* Se renderizan los lotes filtrados */}
                        {lotesFiltrados.length > 0 ? (
                            lotesFiltrados.map(l => (
                                <tr key={l.idLote}>
                                    <td>{l.nombreProducto}</td>
                                    <td>{l.codigoLote}</td>
                                    <td>{new Date(l.fechaVencimiento).toLocaleDateString()}</td>
                                    <td>{l.stockRestante}</td>
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td colSpan={4} style={{ textAlign: 'center' }}>No se encontraron lotes.</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

export default Lotes;