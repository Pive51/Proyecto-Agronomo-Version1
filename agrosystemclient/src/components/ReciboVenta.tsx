import { useState } from 'react';
import jsPDF from 'jspdf';
import api from '../services/api';

interface DetalleReciboItem {
    producto: string;
    cantidad: number;
    precioUnitario: number;
    subtotal: number;
}

interface ReciboVentaProps {
    idVenta: number;
    fecha: string;
    cliente: string;
    identificacionCliente?: string;
    emailCliente?: string;
    detalle: DetalleReciboItem[];
    total: number;
    formaPago: string;
    onCerrar: () => void;
}

export default function ReciboVenta({
    idVenta, fecha, cliente, identificacionCliente, emailCliente,
    detalle, total, formaPago, onCerrar
}: ReciboVentaProps) {
    const [enviarCorreo, setEnviarCorreo] = useState(false);
    const [email, setEmail] = useState('');
    const [enviando, setEnviando] = useState(false);
    const [mensaje, setMensaje] = useState('');
    const [error, setError] = useState('');

    const generarPDF = (): jsPDF => {
        const doc = new jsPDF({ format: [80, 200], unit: 'mm' }); // formato ticket angosto
        let y = 10;

        doc.setFontSize(12);
        doc.setFont('helvetica', 'bold');
        doc.text('Agro Verde', 40, y, { align: 'center' });
        y += 5;
        doc.setFontSize(8);
        doc.setFont('helvetica', 'normal');
        doc.text('Tecnología del norte', 40, y, { align: 'center' });
        y += 6;

        doc.text(`Comprobante de Venta #${idVenta}`, 40, y, { align: 'center' });
        y += 5;
        doc.text(`Fecha: ${new Date(fecha).toLocaleString()}`, 5, y);
        y += 4;
        doc.text(`Cliente: ${cliente}`, 5, y);
        y += 4;
        if (identificacionCliente) {
            doc.text(`Cédula: ${identificacionCliente}`, 5, y);
            y += 4;
        }
        doc.text(`Forma de pago: ${formaPago}`, 5, y);
        y += 6;

        doc.line(5, y, 75, y);
        y += 4;
        doc.setFont('helvetica', 'bold');
        doc.text('Producto', 5, y);
        doc.text('Cant.', 45, y);
        doc.text('Subt.', 65, y);
        doc.setFont('helvetica', 'normal');
        y += 4;
        doc.line(5, y, 75, y);
        y += 4;

        detalle.forEach(d => {
            doc.text(d.producto.substring(0, 20), 5, y);
            doc.text(String(d.cantidad), 45, y);
            doc.text(`$${d.subtotal.toFixed(2)}`, 65, y);
            y += 4;
        });

        y += 2;
        doc.line(5, y, 75, y);
        y += 5;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.text(`TOTAL:`, 5, y);
        doc.text(`$${total.toFixed(2)}`, 65, y);
        y += 8;

        doc.setFontSize(7);
        doc.setFont('helvetica', 'normal');
        doc.text('Este comprobante no tiene validez tributaria (SRI).', 40, y, { align: 'center' });

        return doc;
    };

    const imprimir = () => {
        const doc = generarPDF();
        doc.autoPrint();
        window.open(doc.output('bloburl'), '_blank');
    };

    const descargar = () => {
        const doc = generarPDF();
        doc.save(`Comprobante_Venta_${idVenta}.pdf`);
    };

    const enviarPorCorreo = async () => {
        const destinatario = emailCliente || email.trim();
        if (!destinatario) {
            setError('Este cliente no tiene correo registrado. Ingresa uno manualmente.');
            return;
        }
        setEnviando(true);
        setError('');
        setMensaje('');
        try {
            const doc = generarPDF();
            const pdfBase64 = doc.output('datauristring').split(',')[1];

            const res = await api.post(`/Ventas/${idVenta}/enviar-comprobante`, {
                email: destinatario,
                pdfBase64
            });

            if (res.data.success) {
                setMensaje('Comprobante enviado correctamente.');
            } else {
                setError(res.data.mensaje ?? 'No se pudo enviar el correo.');
            }
        } catch (err: any) {
            setError(err.response?.data?.mensaje ?? 'Error al enviar el comprobante.');
        } finally {
            setEnviando(false);
        }
    };

    return (
        <div className="modal-overlay" onClick={onCerrar}>
            <div className="modal-content" onClick={e => e.stopPropagation()} style={{ maxWidth: 420 }}>
                <div className="modal-header">
                    <h2>Comprobante de Venta #{idVenta}</h2>
                    <button className="btn-close" onClick={onCerrar}>✕</button>
                </div>

                <div style={{ fontSize: 13, color: '#4b5563', marginBottom: 12 }}>
                    <p><strong>Cliente:</strong> {cliente}</p>
                    <p><strong>Fecha:</strong> {new Date(fecha).toLocaleString()}</p>
                    <p><strong>Forma de pago:</strong> {formaPago}</p>
                </div>

                <table className="clientes-table" style={{ marginBottom: 12 }}>
                    <thead>
                        <tr>
                            <th>Producto</th>
                            <th>Cant.</th>
                            <th>Subtotal</th>
                        </tr>
                    </thead>
                    <tbody>
                        {detalle.map((d, i) => (
                            <tr key={i}>
                                <td>{d.producto}</td>
                                <td>{d.cantidad}</td>
                                <td>${d.subtotal.toFixed(2)}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>

                <div style={{ fontSize: 13, marginBottom: 16 }}>
                    <div className="pos-ticket-total"><span>Total</span><span>${total.toFixed(2)}</span></div>
                </div>

                <div className="form-group">
                    <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, cursor: 'pointer' }}>
                        <input
                            type="checkbox"
                            checked={enviarCorreo}
                            onChange={e => setEnviarCorreo(e.target.checked)}
                        />
                        Enviar comprobante por correo electrónico
                    </label>
                </div>

                {enviarCorreo && (
                    <div className="form-group">
                        {emailCliente ? (
                            <div style={{ fontSize: 13, color: '#4b5563', background: '#f3f4f6', padding: '8px 10px', borderRadius: 6 }}>
                                Se enviará a: <strong>{emailCliente}</strong>
                            </div>
                        ) : (
                            <>
                                <p style={{ fontSize: 12, color: '#ef4444', marginBottom: 6 }}>
                                    Este cliente no tiene correo registrado.
                                </p>
                                <input
                                    type="email"
                                    className="form-input"
                                    placeholder="correo@ejemplo.com"
                                    value={email}
                                    onChange={e => setEmail(e.target.value)}
                                />
                            </>
                        )}
                    </div>
                )}

                {error && <div className="error-message">{error}</div>}
                {mensaje && <div className="success-message">{mensaje}</div>}

                <div className="modal-footer" style={{ flexWrap: 'wrap', gap: 8 }}>
                    <button className="btn-cancelar" onClick={onCerrar}>Cerrar</button>
                    <button className="btn-action-text" onClick={descargar}>Descargar PDF</button>
                    <button className="btn-action-text" onClick={imprimir}>Imprimir</button>
                    {enviarCorreo && (
                        <button className="btn-guardar" onClick={enviarPorCorreo} disabled={enviando}>
                            {enviando ? 'Enviando...' : 'Enviar por correo'}
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}