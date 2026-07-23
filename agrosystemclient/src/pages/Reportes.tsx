import { useState, useEffect } from 'react';
import * as XLSX from 'xlsx';
import {
    getReporteMermas,
    getReporteUtilidad,
    getReporteVentasPeriodo,
    getReporteProductosMasVendidos
} from '../services/api';
import api from '../services/api';
import type {
    MermaLote,
    UtilidadProducto,
    VentaPorDia,
    ResumenVentasPeriodo,
    ProductoMasVendido
} from '../interfaces/IReporte';

type Pestana = 'utilidad' | 'mermas' | 'ventasPeriodo' | 'productosMasVendidos';

interface CategoriaOpcion {
    idCategoria: number;
    nombre: string;
}

export default function Reportes() {
    const [pestana, setPestana] = useState<Pestana>('utilidad');
    const [mermas, setMermas] = useState<MermaLote[]>([]);
    const [utilidad, setUtilidad] = useState<UtilidadProducto[]>([]);
    const [diasVentas, setDiasVentas] = useState<VentaPorDia[]>([]);
    const [resumenVentas, setResumenVentas] = useState<ResumenVentasPeriodo | null>(null);
    const [productosMasVendidos, setProductosMasVendidos] = useState<ProductoMasVendido[]>([]);

    const [fechaDesde, setFechaDesde] = useState('');
    const [fechaHasta, setFechaHasta] = useState('');
    const [formaPago, setFormaPago] = useState('');
    const [categorias, setCategorias] = useState<CategoriaOpcion[]>([]);
    const [idCategoria, setIdCategoria] = useState<number | ''>('');

    const [cargando, setCargando] = useState(false);
    const [error, setError] = useState('');

    useEffect(() => {
        api.get('Ventas/Categorias').then(res => {
            if (res.data?.success) setCategorias(res.data.data);
            else if (Array.isArray(res.data)) setCategorias(res.data);
        });
    }, []);

    useEffect(() => {
        cargar();
    }, [pestana, fechaDesde, fechaHasta, formaPago, idCategoria]);

    const cargar = async () => {
        setCargando(true);
        setError('');
        try {
            if (pestana === 'utilidad') {
                const res = await getReporteUtilidad(fechaDesde || undefined, fechaHasta || undefined);
                if (res.success) setUtilidad(res.data);
                else setError(res.mensaje ?? 'Error al cargar el reporte.');
            } else if (pestana === 'mermas') {
                const res = await getReporteMermas(fechaDesde || undefined, fechaHasta || undefined);
                if (res.success) setMermas(res.data);
                else setError(res.mensaje ?? 'Error al cargar el reporte.');
            } else if (pestana === 'ventasPeriodo') {
                const res = await getReporteVentasPeriodo(fechaDesde || undefined, fechaHasta || undefined, formaPago || undefined);
                if (res.success) {
                    setDiasVentas(res.data.dias);
                    setResumenVentas(res.data.resumen);
                } else {
                    setError(res.mensaje ?? 'Error al cargar el reporte.');
                }
            } else {
                const res = await getReporteProductosMasVendidos(
                    fechaDesde || undefined,
                    fechaHasta || undefined,
                    idCategoria === '' ? undefined : Number(idCategoria)
                );
                if (res.success) setProductosMasVendidos(res.data);
                else setError(res.mensaje ?? 'Error al cargar el reporte.');
            }
        } catch (err: any) {
            setError(err.response?.data?.mensaje ?? 'Error al cargar el reporte.');
        } finally {
            setCargando(false);
        }
    };

    const exportarExcel = () => {
        const desde = fechaDesde || "Inicio";
        const hasta = fechaHasta || "Fin";

        let datosExportar: any[] = [];
        let nombreArchivo = "";

        if (pestana === 'utilidad') {
            datosExportar = utilidad.map(u => ({
                "Producto": u.producto,
                "Cant. Vendida": u.cantidadVendida,
                "Total Ventas": u.totalVentas,
                "Costo Total": u.costoTotal,
                "Utilidad": u.utilidad,
                "Margen %": u.margenPorcentaje
            }));
            nombreArchivo = `Reporte_Utilidad_${desde}_al_${hasta}.xlsx`;
        } else if (pestana === 'mermas') {
            datosExportar = mermas.map(m => ({
                "Producto": m.producto,
                "Código Lote": m.codigoLote,
                "Vencimiento": m.fechaVencimiento,
                "Stock Perdido": m.stockRestante,
                "Costo Unitario": m.precioCompra,
                "Valor Perdido": m.valorPerdida
            }));
            nombreArchivo = `Reporte_Mermas_${desde}_al_${hasta}.xlsx`;
        } else if (pestana === 'ventasPeriodo') {
            datosExportar = diasVentas.map(d => ({
                "Fecha": new Date(d.fecha).toLocaleDateString(),
                "# Ventas": d.cantidadVentas,
                "Total Vendido": d.totalVentas,
                "Ticket Promedio": d.ticketPromedio
            }));
            nombreArchivo = `Reporte_VentasPeriodo_${desde}_al_${hasta}.xlsx`;
        } else {
            datosExportar = productosMasVendidos.map(p => ({
                "Producto": p.producto,
                "Categoría": p.categoria ?? '',
                "Cantidad Vendida": p.cantidadVendida,
                "Total Ventas": p.totalVentas
            }));
            nombreArchivo = `Reporte_ProductosMasVendidos_${desde}_al_${hasta}.xlsx`;
        }

        const ws = XLSX.utils.json_to_sheet(datosExportar);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, "Reporte");
        XLSX.writeFile(wb, nombreArchivo);
    };

    

    const BotonPestana = ({ valor, texto }: { valor: Pestana; texto: string }) => (
        <button
            className="btn"
            style={{ backgroundColor: pestana === valor ? '#115e59' : '#f3f4f6', color: pestana === valor ? '#fff' : '#4b5563' }}
            onClick={() => setPestana(valor)}
        >
            {texto}
        </button>
    );

    return (
        <div className="clientes-wrapper">
            <div className="clientes-header">
                <h2 className="clientes-title">Reportes</h2>
            </div>

            <div className="flex-row" style={{ gap: 10, marginBottom: 16, justifyContent: 'space-between', flexWrap: 'wrap' }}>
                <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                    <BotonPestana valor="utilidad" texto="Utilidad por Producto" />
                    <BotonPestana valor="mermas" texto="Mermas por Caducidad" />
                    <BotonPestana valor="ventasPeriodo" texto="Ventas por Período" />
                    <BotonPestana valor="productosMasVendidos" texto="Productos Más Vendidos" />
                </div>
                <button className="btn" onClick={exportarExcel} style={{ backgroundColor: '#059669', color: '#fff' }}>
                    Exportar a Excel
                </button>
            </div>

            <div className="control-box" style={{ marginBottom: 16 }}>
                <div className="filters-group">
                    <div>
                        <span className="sub-label">Desde</span>
                        <input type="date" className="filter-select" value={fechaDesde} onChange={e => setFechaDesde(e.target.value)} />
                    </div>
                    <div>
                        <span className="sub-label">Hasta</span>
                        <input type="date" className="filter-select" value={fechaHasta} onChange={e => setFechaHasta(e.target.value)} />
                    </div>

                    {pestana === 'ventasPeriodo' && (
                        <div>
                            <span className="sub-label">Forma de Pago</span>
                            <select className="filter-select" value={formaPago} onChange={e => setFormaPago(e.target.value)}>
                                <option value="">Todas</option>
                                <option value="EFECTIVO">Efectivo</option>
                                <option value="TRANSFERENCIA">Transferencia</option>
                            </select>
                        </div>
                    )}

                    {pestana === 'productosMasVendidos' && (
                        <div>
                            <span className="sub-label">Categoría</span>
                            <select
                                className="filter-select"
                                value={idCategoria}
                                onChange={e => setIdCategoria(e.target.value === '' ? '' : Number(e.target.value))}
                            >
                                <option value="">Todas</option>
                                {categorias.map(c => (
                                    <option key={c.idCategoria} value={c.idCategoria}>{c.nombre}</option>
                                ))}
                            </select>
                        </div>
                    )}
                </div>
            </div>

            {error && <div className="error-message">{error}</div>}

            <div className="table-container">
                {cargando ? (
                    <p className="pos-empty">Cargando...</p>
                ) : pestana === 'utilidad' ? (
                    <table className="clientes-table">
                        <thead>
                            <tr>
                                <th>Producto</th><th>Cant. Vendida</th><th>Total Ventas</th><th>Costo Total</th><th>Utilidad</th><th>Margen %</th>
                            </tr>
                        </thead>
                        <tbody>
                            {utilidad.map(u => (
                                <tr key={u.idProducto}>
                                    <td>{u.producto}</td>
                                    <td>{u.cantidadVendida}</td>
                                    <td>${u.totalVentas.toFixed(2)}</td>
                                    <td>${u.costoTotal.toFixed(2)}</td>
                                    <td>${u.utilidad.toFixed(2)}</td>
                                    <td>{u.margenPorcentaje.toFixed(2)}%</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                ) : pestana === 'mermas' ? (
                    <table className="clientes-table">
                        <thead>
                            <tr>
                                <th>Producto</th><th>Código de Lote</th><th>Fecha Vencimiento</th><th>Stock Perdido</th><th>Costo Unitario</th><th>Valor Perdido</th>
                            </tr>
                        </thead>
                        <tbody>
                            {mermas.map(m => (
                                <tr key={m.idLote}>
                                    <td>{m.producto}</td>
                                    <td>{m.codigoLote}</td>
                                    <td>{new Date(m.fechaVencimiento).toLocaleDateString()}</td>
                                    <td>{m.stockRestante}</td>
                                    <td>${m.precioCompra.toFixed(2)}</td>
                                    <td>${m.valorPerdida.toFixed(2)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                ) : pestana === 'ventasPeriodo' ? (
                    <>
                        {resumenVentas && (
                            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: 12, marginBottom: 20 }}>
                                <div style={{ background: '#f5f5f2', borderRadius: 8, padding: '1rem' }}>
                                    <p style={{ fontSize: 13, color: '#6c757d', margin: '0 0 4px' }}>Total del período</p>
                                    <p style={{ fontSize: 22, fontWeight: 600, margin: 0 }}>${resumenVentas.totalActual.toFixed(2)}</p>
                                </div>
                                <div style={{ background: '#f5f5f2', borderRadius: 8, padding: '1rem' }}>
                                    <p style={{ fontSize: 13, color: '#6c757d', margin: '0 0 4px' }}>vs. período anterior</p>
                                    <p style={{
                                        fontSize: 22, fontWeight: 600, margin: 0,
                                        color: resumenVentas.variacionPorcentaje == null ? '#6c757d' : resumenVentas.variacionPorcentaje >= 0 ? '#16a34a' : '#dc2626'
                                    }}>
                                        {resumenVentas.variacionPorcentaje == null
                                            ? 'N/A'
                                            : `${resumenVentas.variacionPorcentaje >= 0 ? '+' : ''}${resumenVentas.variacionPorcentaje.toFixed(1)}%`}
                                    </p>
                                    <p style={{ fontSize: 11, color: '#9ca3af', margin: '4px 0 0' }}>
                                        anterior: ${resumenVentas.totalAnterior.toFixed(2)}
                                    </p>
                                </div>
                                <div style={{ background: '#f5f5f2', borderRadius: 8, padding: '1rem' }}>
                                    <p style={{ fontSize: 13, color: '#6c757d', margin: '0 0 4px' }}># de Ventas</p>
                                    <p style={{ fontSize: 22, fontWeight: 600, margin: 0 }}>{resumenVentas.cantidadVentas}</p>
                                </div>
                                <div style={{ background: '#f5f5f2', borderRadius: 8, padding: '1rem' }}>
                                    <p style={{ fontSize: 13, color: '#6c757d', margin: '0 0 4px' }}>Ticket Promedio</p>
                                    <p style={{ fontSize: 22, fontWeight: 600, margin: 0 }}>${resumenVentas.ticketPromedio.toFixed(2)}</p>
                                </div>
                            </div>
                        )}
                        <table className="clientes-table">
                            <thead>
                                <tr>
                                    <th>Fecha</th><th># Ventas</th><th>Total Vendido</th><th>Ticket Promedio</th>
                                </tr>
                            </thead>
                            <tbody>
                                {diasVentas.length === 0 ? (
                                    <tr><td colSpan={4} style={{ textAlign: 'center', color: '#9ca3af', padding: 20 }}>No hay ventas en este período.</td></tr>
                                ) : (
                                    diasVentas.map(d => (
                                        <tr key={d.fecha}>
                                            <td>{new Date(d.fecha).toLocaleDateString()}</td>
                                            <td>{d.cantidadVentas}</td>
                                            <td>${d.totalVentas.toFixed(2)}</td>
                                            <td>${d.ticketPromedio.toFixed(2)}</td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </>
                ) : (
                    <table className="clientes-table">
                        <thead>
                            <tr>
                                <th>#</th><th>Producto</th><th>Categoría</th><th>Cantidad Vendida</th><th>Total Ventas</th>
                            </tr>
                        </thead>
                        <tbody>
                            {productosMasVendidos.length === 0 ? (
                                <tr><td colSpan={5} style={{ textAlign: 'center', color: '#9ca3af', padding: 20 }}>No hay ventas en este período.</td></tr>
                            ) : (
                                productosMasVendidos.map((p, i) => (
                                    <tr key={p.idProducto}>
                                        <td>{i + 1}</td>
                                        <td>{p.producto}</td>
                                        <td>{p.categoria ?? '-'}</td>
                                        <td>{p.cantidadVendida}</td>
                                        <td>${p.totalVentas.toFixed(2)}</td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}