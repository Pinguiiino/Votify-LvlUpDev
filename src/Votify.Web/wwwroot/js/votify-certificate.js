// wwwroot/js/votify-certificate.js
// Generación de certificado PDF en el frontend con jsPDF.
// Soporta múltiples clasificaciones (varias categorías, Jurado/Público).
//
// IMPORTANTE: usa blob + link en vez de doc.save() para evitar que el
// diálogo nativo "Guardar como" pause la pestaña y rompa SignalR.

window.votifyCertificate = {
    generar: function (datos) {
        const { jsPDF } = window.jspdf;

        const doc = new jsPDF({
            orientation: 'landscape',
            unit: 'mm',
            format: 'a4'
        });

        const pageWidth = doc.internal.pageSize.getWidth();    // 297 mm
        const pageHeight = doc.internal.pageSize.getHeight();  // 210 mm

        // ¿Es ganador en alguna clasificación? (TOP 3)
        const clasificaciones = datos.clasificaciones || [];
        const ganadoras = clasificaciones.filter(c => c.puesto != null && c.puesto <= 3);
        const esGanador = ganadoras.length > 0;

        // ─── Fondo ───────────────────────────────────────────────
        if (esGanador) {
            doc.setFillColor(250, 238, 218); // crema (#FAEEDA)
        } else {
            doc.setFillColor(238, 237, 254); // lavanda (#EEEDFE)
        }
        doc.rect(0, 0, pageWidth, pageHeight, 'F');

        // ─── Marco doble ─────────────────────────────────────────
        const marcoColor = esGanador ? [186, 117, 23] : [83, 74, 183];
        doc.setDrawColor(...marcoColor);
        doc.setLineWidth(2);
        doc.rect(8, 8, pageWidth - 16, pageHeight - 16);
        doc.setLineWidth(0.5);
        doc.rect(12, 12, pageWidth - 24, pageHeight - 24);

        // ─── Título ──────────────────────────────────────────────
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(28);
        doc.setTextColor(...marcoColor);
        const titulo = esGanador
            ? 'CERTIFICADO DE RECONOCIMIENTO'
            : 'CERTIFICADO DE PARTICIPACIÓN';
        doc.text(titulo, pageWidth / 2, 40, { align: 'center' });

        // ─── Texto introductorio ─────────────────────────────────
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(14);
        doc.setTextColor(60, 52, 137);
        doc.text('Se otorga el presente certificado a:', pageWidth / 2, 60, { align: 'center' });

        // ─── Nombre del participante ─────────────────────────────
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(26);
        doc.setTextColor(60, 52, 137);
        doc.text(datos.nombreParticipante, pageWidth / 2, 80, { align: 'center' });

        // ─── Clasificaciones (si es ganador, mostrar las top) ─────
        let yActual = 100;
        if (esGanador) {
            doc.setFont('helvetica', 'bold');
            doc.setFontSize(14);
            doc.setTextColor(186, 117, 23);

            const ordinal = (n) => {
                if (n === 1) return '1.er puesto';
                return `${n}.º puesto`;
            };

            // Si solo hay una ganadora, una línea simple
            if (ganadoras.length === 1) {
                const g = ganadoras[0];
                const linea = `${g.tipo} · ${ordinal(g.puesto)} en ${g.categoria}`;
                doc.text(linea, pageWidth / 2, yActual, { align: 'center' });
                yActual += 12;
            } else {
                // Múltiples → una línea por clasificación
                for (const g of ganadoras) {
                    const linea = `${g.tipo}: ${ordinal(g.puesto)} en ${g.categoria}`;
                    doc.text(linea, pageWidth / 2, yActual, { align: 'center' });
                    yActual += 7;
                }
                yActual += 5;
            }
        }

        // ─── Texto del evento ────────────────────────────────────
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(13);
        doc.setTextColor(60, 52, 137);
        const textoParticipacion = esGanador
            ? 'Por su destacada participación con el proyecto'
            : 'Por su participación con el proyecto';
        doc.text(textoParticipacion, pageWidth / 2, yActual, { align: 'center' });
        yActual += 10;

        // Nombre del proyecto
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(15);
        doc.setTextColor(15, 23, 42);
        doc.text(`"${datos.nombreProyecto}"`, pageWidth / 2, yActual, { align: 'center' });
        yActual += 10;

        // Evento
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(13);
        doc.setTextColor(60, 52, 137);
        doc.text(`en el evento ${datos.nombreEvento}`, pageWidth / 2, yActual, { align: 'center' });

        // ─── Fecha ───────────────────────────────────────────────
        doc.setFontSize(11);
        doc.setTextColor(100, 100, 120);
        doc.text(`Fecha: ${datos.fechaEvento}`, pageWidth / 2, pageHeight - 35, { align: 'center' });

        // ─── Pie / firma ─────────────────────────────────────────
        doc.setDrawColor(150, 150, 170);
        doc.setLineWidth(0.3);
        doc.line(pageWidth / 2 - 40, pageHeight - 22, pageWidth / 2 + 40, pageHeight - 22);
        doc.setFontSize(10);
        doc.setTextColor(120, 120, 140);
        doc.text('Firma autorizada · Votify', pageWidth / 2, pageHeight - 17, { align: 'center' });

        // ─── Descargar (sin diálogo "Guardar como" bloqueante) ────
        const nombreArchivo = `Certificado_${datos.nombreProyecto.replace(/[^a-zA-Z0-9]/g, '_')}.pdf`;
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