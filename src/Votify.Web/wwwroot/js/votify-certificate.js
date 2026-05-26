// wwwroot/js/votify-certificate.js
// Generación de certificado PDF en el frontend con jsPDF.
// Diseño clásico tipo diploma con paletas diferenciadas:
//   - Ganador (TOP 3): burdeos + oro sobre fondo crema cálido
//   - Participación:    gris carbón sobre fondo crema neutro

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

        // ─── Detectar si es ganador y elegir la "mejor" clasificación ──
        const clasificaciones = datos.clasificaciones || [];
        const ganadoras = clasificaciones
            .filter(c => c.puesto != null && c.puesto <= 3)
            .sort((a, b) => a.puesto - b.puesto);
        const esGanador = ganadoras.length > 0;
        const principal = esGanador ? ganadoras[0] : (clasificaciones[0] || null);

        // ─── Paleta según ganador o participante ─────────────────────────
        const paleta = esGanador ? {
            fondo: [251, 248, 241],   // crema cálido
            marcoFuerte: [122, 31, 44],     // burdeos
            marcoFino: [186, 117, 23],    // oro
            tituloFuerte: [122, 31, 44],
            acento: [186, 117, 23],
            texto: [44, 44, 42],
            textoSuave: [95, 94, 90],
            cajaFondo: [250, 238, 218]    // crema oro claro
        } : {
            fondo: [250, 249, 246],   // crema neutro
            marcoFuerte: [44, 44, 42],      // carbón
            marcoFino: [136, 135, 128],   // gris medio
            tituloFuerte: [44, 44, 42],
            acento: [95, 94, 90],
            texto: [44, 44, 42],
            textoSuave: [95, 94, 90],
            cajaFondo: [241, 239, 232]    // gris muy claro
        };

        // Helpers
        const setFill = c => doc.setFillColor(c[0], c[1], c[2]);
        const setDraw = c => doc.setDrawColor(c[0], c[1], c[2]);
        const setText = c => doc.setTextColor(c[0], c[1], c[2]);

        // ═══════════════════════════════════════════════════════════════
        // LAYOUT (página de 210mm de alto, marco desde y=8 hasta y=202)
        //
        // Y=20  → separador ornamental superior
        // Y=30  → nombre del evento
        // Y=55  → "DIPLOMA" (texto grande, base en 55)
        // Y=68  → subtítulo del puesto
        // Y=78  → separador ornamental medio
        // Y=88  → "otorgado a"
        // Y=108 → nombre del participante (grande)
        // Y=115 → línea bajo el nombre
        // Y=127 → "por su... participación con el proyecto"
        // Y=138 → "nombreProyecto" en negrita
        // Y=147 → categoría (si ganador)
        // Y=160 → recuadro con la fecha
        // Y=190 → "VOTIFY"
        // ═══════════════════════════════════════════════════════════════

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

        // ─── Helper: separador ornamental ───────────────────────────────
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

        // ─── 4. SEPARADOR SUPERIOR (y=20) ────────────────────────────────
        dibujarSeparador(20, 3, 13, 0.9);

        // ─── 5. NOMBRE DEL EVENTO (y=30) ─────────────────────────────────
        doc.setFont('times', 'normal');
        doc.setFontSize(12);
        setText(paleta.tituloFuerte);
        doc.text(
            (datos.nombreEvento || '').toUpperCase(),
            cx, 30,
            { align: 'center', charSpace: 0 }
        );

        // ─── 6. "DIPLOMA" GIGANTE (base y=62) ────────────────────────────
        doc.setFont('times', 'bold');
        doc.setFontSize(60);
        setText(paleta.tituloFuerte);
        doc.text('DIPLOMA', cx, 62, { align: 'center', charSpace: 1.5 });

        // Subtítulo del puesto (y=72)
        doc.setFont('times', 'normal');
        doc.setFontSize(14);
        setText(paleta.tituloFuerte);
        const ordinal = (n) => {
            if (n === 1) return 'PRIMER PUESTO';
            if (n === 2) return 'SEGUNDO PUESTO';
            if (n === 3) return 'TERCER PUESTO';
            return `${n}.º PUESTO`;
        };
        const subtitulo = esGanador && principal
            ? ordinal(principal.puesto)
            : 'PARTICIPACIÓN';
        doc.text(subtitulo, cx, 72, { align: 'center', charSpace: 0 });

        // ─── 7. SEPARADOR MEDIO (y=80) ───────────────────────────────────
        dibujarSeparador(80, 4, 20, 1);

        // ─── 8. "otorgado a" (y=90) ──────────────────────────────────────
        doc.setFont('times', 'italic');
        doc.setFontSize(13);
        setText(paleta.textoSuave);
        doc.text('otorgado a', cx, 90, { align: 'center' });

        // ─── 9. NOMBRE DEL PARTICIPANTE (y=110) ──────────────────────────
        doc.setFont('times', 'italic');
        doc.setFontSize(32);
        setText(paleta.texto);
        doc.text(datos.nombreParticipante || '', cx, 110, { align: 'center' });

        // Línea fina bajo el nombre (y=116)
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

        // Nombre del proyecto en negrita (y=140)
        doc.setFont('times', 'bold');
        doc.setFontSize(16);
        setText(paleta.texto);
        doc.text(`"${datos.nombreProyecto || ''}"`, cx, 140, { align: 'center' });

        // Categoría + tipo (solo para ganadores, y=149)
        let yExtras = 149;
        if (esGanador && principal) {
            doc.setFont('times', 'italic');
            doc.setFontSize(11);
            setText(paleta.textoSuave);
            const linea3 = `en la categoría ${principal.categoria} · ${principal.tipo.toLowerCase()}`;
            doc.text(linea3, cx, yExtras, { align: 'center' });
            yExtras += 5;

            // Si hay más clasificaciones ganadoras, las listamos en pequeño
            if (ganadoras.length > 1) {
                doc.setFontSize(9);
                for (let i = 1; i < ganadoras.length; i++) {
                    const g = ganadoras[i];
                    const txt = `también ${ordinal(g.puesto).toLowerCase()} en ${g.categoria} · ${g.tipo.toLowerCase()}`;
                    doc.text(txt, cx, yExtras, { align: 'center' });
                    yExtras += 4;
                }
            }
        }

        // ─── 11. RECUADRO CON LA FECHA (y=168) ───────────────────────────
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

        // ─── 12. PIE: VOTIFY (y=192) ─────────────────────────────────────
        setDraw(paleta.acento);
        doc.setLineWidth(0.2);
        doc.line(cx - 25, 188, cx + 25, 188);

        doc.setFont('times', 'normal');
        doc.setFontSize(10);
        setText(paleta.tituloFuerte);
        doc.text('VOTIFY', cx, 193, { align: 'center', charSpace: 3 });

        // ─── 13. DESCARGA (sin diálogo "Guardar como" bloqueante) ────────
        const safeName = (datos.nombreProyecto || 'Certificado').replace(/[^a-zA-Z0-9]/g, '_');
        const nombreArchivo = `Certificado_${safeName}.pdf`;
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