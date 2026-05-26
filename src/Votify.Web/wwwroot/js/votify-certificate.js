// wwwroot/js/votify-certificate.js
// Generación de certificado PDF en el frontend con jsPDF.
// Cada llamada genera UN certificado para UNA sesión cerrada concreta
// (una categoría + un tipo: jurado o público).

window.votifyCertificate = {
    generar: function (datos) {
        const { jsPDF } = window.jspdf;

        const doc = new jsPDF({
            orientation: 'landscape',
            unit: 'mm',
            format: 'a4'
        });

        const pageW = doc.internal.pageSize.getWidth();    // 297 mm
        const pageH = doc.internal.pageSize.getHeight();   // 210 mm
        const cx = pageW / 2;

        // ─── Es ganador si el puesto es TOP 3 en esta sesión ─────────────
        const puesto = datos.puesto;
        const esGanador = puesto != null && puesto >= 1 && puesto <= 3;

        // ─── Paleta ──────────────────────────────────────────────────────
        const paleta = esGanador ? {
            fondo: [251, 248, 241],
            marcoFuerte: [122, 31, 44],
            marcoFino: [186, 117, 23],
            tituloFuerte: [122, 31, 44],
            acento: [186, 117, 23],
            texto: [44, 44, 42],
            textoSuave: [95, 94, 90],
            cajaFondo: [250, 238, 218]
        } : {
            fondo: [250, 249, 246],
            marcoFuerte: [44, 44, 42],
            marcoFino: [136, 135, 128],
            tituloFuerte: [44, 44, 42],
            acento: [95, 94, 90],
            texto: [44, 44, 42],
            textoSuave: [95, 94, 90],
            cajaFondo: [241, 239, 232]
        };

        const setFill = c => doc.setFillColor(c[0], c[1], c[2]);
        const setDraw = c => doc.setDrawColor(c[0], c[1], c[2]);
        const setText = c => doc.setTextColor(c[0], c[1], c[2]);

        // ─── 1. FONDO ────────────────────────────────────────────────────
        setFill(paleta.fondo);
        doc.rect(0, 0, pageW, pageH, 'F');

        // ─── 2. TRIPLE MARCO ─────────────────────────────────────────────
        setDraw(paleta.marcoFuerte);
        doc.setLineWidth(0.8);
        doc.rect(8, 8, pageW - 16, pageH - 16);

        setDraw(paleta.marcoFino);
        doc.setLineWidth(0.3);
        doc.rect(11, 11, pageW - 22, pageH - 22);
        doc.setLineWidth(0.15);
        doc.rect(13, 13, pageW - 26, pageH - 26);

        // ─── 3. ORNAMENTOS EN LAS 4 ESQUINAS ─────────────────────────────
        const dibujarEsquina = (x, y, dirX, dirY) => {
            setDraw(paleta.marcoFino);
            doc.setLineWidth(0.35);
            const len = 8;
            doc.line(x, y, x + dirX * len, y);
            doc.line(x, y, x, y + dirY * len);
            setFill(paleta.acento);
            doc.circle(x + dirX * 2, y + dirY * 2, 0.6, 'F');
            doc.circle(x + dirX * 5, y + dirY * 5, 0.4, 'F');
        };
        const m = 18;
        dibujarEsquina(m, m, 1, 1);
        dibujarEsquina(pageW - m, m, -1, 1);
        dibujarEsquina(m, pageH - m, 1, -1);
        dibujarEsquina(pageW - m, pageH - m, -1, -1);

        // Helper: separador ornamental
        const dibujarSeparador = (y, anchoInt, anchoExt, radio) => {
            setDraw(paleta.acento);
            doc.setLineWidth(0.2);
            doc.line(cx - anchoExt, y, cx - anchoInt, y);
            doc.line(cx + anchoInt, y, cx + anchoExt, y);
            setFill(paleta.acento);
            doc.circle(cx, y, radio, 'F');
            doc.circle(cx - anchoInt - 2, y, radio * 0.55, 'F');
            doc.circle(cx + anchoInt + 2, y, radio * 0.55, 'F');
        };

        // ─── 4. SEPARADOR SUPERIOR ───────────────────────────────────────
        dibujarSeparador(20, 3, 13, 0.9);

        // ─── 5. NOMBRE DEL EVENTO (con espacios manuales) ────────────────
        doc.setFont('times', 'normal');
        doc.setFontSize(12);
        setText(paleta.tituloFuerte);
        const eventoEspaciado = (datos.nombreEvento || '').toUpperCase().split('').join(' ');
        doc.text(eventoEspaciado, cx, 30, { align: 'center' });

        // ─── 6. "DIPLOMA" GIGANTE ────────────────────────────────────────
        doc.setFont('times', 'bold');
        doc.setFontSize(60);
        setText(paleta.tituloFuerte);
        doc.text('DIPLOMA', cx, 62, { align: 'center' });

        // Subtítulo del puesto (con espacios manuales para centrado correcto)
        doc.setFont('times', 'normal');
        doc.setFontSize(14);
        setText(paleta.tituloFuerte);
        const ordinal = (n) => {
            if (n === 1) return 'PRIMER PUESTO';
            if (n === 2) return 'SEGUNDO PUESTO';
            if (n === 3) return 'TERCER PUESTO';
            return `${n}.º PUESTO`;
        };
        const subtitulo = esGanador ? ordinal(puesto) : 'PARTICIPACIÓN';
        const subtituloEspaciado = subtitulo.split('').join(' ');
        doc.text(subtituloEspaciado, cx, 72, { align: 'center' });

        // ─── 7. SEPARADOR MEDIO ──────────────────────────────────────────
        dibujarSeparador(80, 4, 20, 1);

        // ─── 8. "otorgado a" ─────────────────────────────────────────────
        doc.setFont('times', 'italic');
        doc.setFontSize(13);
        setText(paleta.textoSuave);
        doc.text('otorgado a', cx, 90, { align: 'center' });

        // ─── 9. NOMBRE DEL PARTICIPANTE ──────────────────────────────────
        doc.setFont('times', 'italic');
        doc.setFontSize(32);
        setText(paleta.texto);
        doc.text(datos.nombreParticipante || '', cx, 110, { align: 'center' });

        setDraw(paleta.acento);
        doc.setLineWidth(0.25);
        doc.line(cx - 65, 116, cx + 65, 116);

        // ─── 10. DESCRIPCIÓN DEL LOGRO ───────────────────────────────────
        doc.setFont('times', 'italic');
        doc.setFontSize(12);
        setText(paleta.textoSuave);
        const lineaIntro = esGanador
            ? 'por su destacada participación con el proyecto'
            : 'por su participación con el proyecto';
        doc.text(lineaIntro, cx, 130, { align: 'center' });

        // Nombre del proyecto en negrita
        doc.setFont('times', 'bold');
        doc.setFontSize(16);
        setText(paleta.texto);
        doc.text(`"${datos.nombreProyecto || ''}"`, cx, 140, { align: 'center' });

        // Categoría + tipo (siempre, tanto para ganador como para participante)
        doc.setFont('times', 'italic');
        doc.setFontSize(11);
        setText(paleta.textoSuave);
        const tipoTexto = (datos.tipo || '').toLowerCase();
        const lineaCat = `en la categoría ${datos.categoria || ''} · ${tipoTexto}`;
        doc.text(lineaCat, cx, 149, { align: 'center' });

        // ─── 11. RECUADRO CON LA FECHA ───────────────────────────────────
        const fechaY = 168;
        const fechaW = 60;
        const fechaH = 11;
        setFill(paleta.cajaFondo);
        setDraw(paleta.acento);
        doc.setLineWidth(0.25);
        doc.rect(cx - fechaW / 2, fechaY, fechaW, fechaH, 'FD');

        doc.setFont('times', 'italic');
        doc.setFontSize(11);
        setText(paleta.tituloFuerte);
        doc.text(datos.fechaEvento || '', cx, fechaY + 7, { align: 'center' });

        // ─── 12. PIE: VOTIFY ─────────────────────────────────────────────
        setDraw(paleta.acento);
        doc.setLineWidth(0.2);
        doc.line(cx - 25, 188, cx + 25, 188);

        doc.setFont('times', 'normal');
        doc.setFontSize(10);
        setText(paleta.tituloFuerte);
        const votifyEspaciado = 'V O T I F Y';
        doc.text(votifyEspaciado, cx, 193, { align: 'center' });

        // ─── 13. DESCARGA ────────────────────────────────────────────────
        const safeProyecto = (datos.nombreProyecto || 'Certificado').replace(/[^a-zA-Z0-9]/g, '_');
        const safeCategoria = (datos.categoria || '').replace(/[^a-zA-Z0-9]/g, '_');
        const safeTipo = (datos.tipo || '').replace(/[^a-zA-Z0-9]/g, '_');
        const nombreArchivo = `Certificado_${safeProyecto}_${safeCategoria}_${safeTipo}.pdf`;

        const blob = doc.output('blob');
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = nombreArchivo;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 100);
    }
};