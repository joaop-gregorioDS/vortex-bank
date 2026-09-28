package com.vortexsoftware.vortexbank.ui

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import androidx.core.content.FileProvider
import androidx.core.content.res.ResourcesCompat
import com.vortexsoftware.vortexbank.R
import com.vortexsoftware.vortexbank.data.StatementDocument
import com.vortexsoftware.vortexbank.data.dayTitle
import com.vortexsoftware.vortexbank.data.kindLabel
import com.vortexsoftware.vortexbank.data.money
import com.vortexsoftware.vortexbank.data.whenLabel
import java.io.File

fun shareStatement(context: Context, document: StatementDocument) {
    val file = writeStatement(context, document)
    val uri = FileProvider.getUriForFile(context, "${context.packageName}.files", file)
    val send = Intent(Intent.ACTION_SEND).apply {
        type = "application/pdf"
        putExtra(Intent.EXTRA_STREAM, uri)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    context.startActivity(Intent.createChooser(send, "Compartilhar extrato"))
}

fun writeStatement(context: Context, document: StatementDocument): File {
    val dir = File(context.cacheDir, "statements").apply { mkdirs() }
    val safe = document.number.filter { it.isLetterOrDigit() }.ifBlank { "conta" }
    val file = File(dir, "extrato-vortex-$safe.pdf")
    val regular = ResourcesCompat.getFont(context, R.font.manrope_regular) ?: Typeface.SANS_SERIF
    val bold = ResourcesCompat.getFont(context, R.font.manrope_bold) ?: Typeface.DEFAULT_BOLD
    val pdf = PdfDocument()
    val pageWidth = 595
    val pageHeight = 842
    var page = pdf.startPage(PdfDocument.PageInfo.Builder(pageWidth, pageHeight, 1).create())
    var canvas = page.canvas
    var y = 48f
    val ink = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(28, 20, 24); textSize = 16f; typeface = bold }
    val body = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(28, 20, 24); textSize = 11f; typeface = regular }
    val muted = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(109, 97, 104); textSize = 9f; typeface = regular }
    val debit = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(225, 29, 72); textSize = 11f; typeface = bold; textAlign = Paint.Align.RIGHT }
    val credit = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(21, 122, 69); textSize = 11f; typeface = bold; textAlign = Paint.Align.RIGHT }
    val right = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(28, 20, 24); textSize = 11f; typeface = bold; textAlign = Paint.Align.RIGHT }
    val rightMuted = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = Color.rgb(109, 97, 104); textSize = 9f; typeface = regular; textAlign = Paint.Align.RIGHT }
    val rule = Paint().apply { color = Color.rgb(225, 29, 72); strokeWidth = 1.5f }

    fun finish() {
        pdf.finishPage(page)
    }

    fun nextPage() {
        finish()
        page = pdf.startPage(PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pdf.pages.size + 1).create())
        canvas = page.canvas
        y = 48f
    }

    fun ensure(need: Float) {
        if (y + need > pageHeight - 48f) nextPage()
    }

    canvas.drawText("Vortex Bank", 40f, y, ink)
    rightMuted.textSize = 10f
    canvas.drawText(document.holder, pageWidth - 40f, y, rightMuted)
    y += 16f
    canvas.drawText("EXTRATO DA CONTA", 40f, y, muted)
    canvas.drawText("CPF ${document.cpf}", pageWidth - 40f, y, rightMuted)
    y += 14f
    canvas.drawText("Ag. 0001  ·  Cc. ${document.number}  ·  ${document.account}", pageWidth - 40f, y, rightMuted)
    y += 14f
    canvas.drawText("Emitido em ${document.issuedAt}", pageWidth - 40f, y, rightMuted)
    y += 10f
    canvas.drawLine(40f, y, pageWidth - 40f, y, rule)
    y += 22f

    for (group in document.groups) {
        ensure(36f)
        val bar = Paint().apply { color = Color.rgb(244, 238, 240) }
        canvas.drawRect(40f, y - 12f, pageWidth - 40f, y + 6f, bar)
        canvas.drawText(dayTitle(group.date), 48f, y, body.apply { typeface = bold })
        body.typeface = regular
        y += 20f
        canvas.drawText("Saldo do dia", 48f, y, body)
        canvas.drawText(money(group.balance), pageWidth - 40f, y, right)
        y += 16f
        for (line in group.lines) {
            ensure(28f)
            val title = "${kindLabel(line.kind)} - ${if (line.direction == "debito") "Enviado" else "Recebido"}"
            canvas.drawText(title.take(48), 48f, y, body)
            val amountPaint = if (line.direction == "debito") debit else credit
            canvas.drawText("${if (line.direction == "debito") "-" else "+"} ${money(line.amount)}", pageWidth - 40f, y, amountPaint)
            y += 13f
            canvas.drawText("${whenLabel(line)}  ${line.description}".take(78), 48f, y, muted)
            y += 16f
        }
        y += 6f
    }

    ensure(48f)
    y += 8f
    val center = Paint(body).apply { textAlign = Paint.Align.CENTER; typeface = bold }
    canvas.drawText("Entradas ${document.credits}    Saídas ${document.debits}", pageWidth / 2f, y, center)
    y += 22f
    val footer = "Vortex Software · documento de demonstração, não é extrato de instituição real."
    val width = pageWidth - 80f
    var rest = footer
    val footerPaint = Paint(muted)
    while (rest.isNotEmpty()) {
        ensure(14f)
        val count = footerPaint.breakText(rest, true, width, null).coerceAtLeast(1)
        canvas.drawText(rest.substring(0, count), 40f, y, footerPaint)
        y += 12f
        rest = rest.substring(count).trimStart()
    }
    finish()
    file.outputStream().use { pdf.writeTo(it) }
    pdf.close()
    return file
}
