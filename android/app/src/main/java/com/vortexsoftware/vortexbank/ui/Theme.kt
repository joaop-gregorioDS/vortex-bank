package com.vortexsoftware.vortexbank.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.sp
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.material3.LocalContentColor
import com.vortexsoftware.vortexbank.R

val Rose = Color(0xFFE11D48)
val RosePressed = Color(0xFFBE123C)
val Ink = Color(0xFF1C1418)
val Muted = Color(0xFF6D6168)
val Canvas = Color(0xFFF7F2F4)
val Credit = Color(0xFF157A45)
val Line = Color(0xFFEADFE4)
val RoseSoft = Color(0xFFFB7185)
val Wine = Color(0xFF9F1239)
val Blush = Color(0xFFFFF1F4)
val Field = Color(0xFFFBF7F8)
val DayBar = Color(0xFFF4EEF0)

val Manrope = FontFamily(
    Font(R.font.manrope_regular, FontWeight.Normal),
    Font(R.font.manrope_medium, FontWeight.Medium),
    Font(R.font.manrope_semibold, FontWeight.SemiBold),
    Font(R.font.manrope_bold, FontWeight.Bold),
    Font(R.font.manrope_extrabold, FontWeight.ExtraBold),
)

private val Base = TextStyle(fontFamily = Manrope, color = Ink)

val VortexTypography = Typography(
    displayLarge = Base.copy(fontSize = 40.sp, fontWeight = FontWeight.ExtraBold, letterSpacing = (-1.2).sp),
    headlineLarge = Base.copy(fontSize = 28.sp, fontWeight = FontWeight.ExtraBold, letterSpacing = (-0.6).sp),
    headlineMedium = Base.copy(fontSize = 22.sp, fontWeight = FontWeight.Bold),
    titleLarge = Base.copy(fontSize = 18.sp, fontWeight = FontWeight.Bold),
    titleMedium = Base.copy(fontSize = 16.sp, fontWeight = FontWeight.Bold),
    bodyLarge = Base.copy(fontSize = 16.sp, lineHeight = 22.sp),
    bodyMedium = Base.copy(fontSize = 14.sp, lineHeight = 20.sp),
    bodySmall = Base.copy(fontSize = 12.sp, lineHeight = 16.sp, color = Muted),
    labelLarge = Base.copy(fontSize = 14.sp, fontWeight = FontWeight.Bold),
    labelMedium = Base.copy(fontSize = 12.sp, fontWeight = FontWeight.Bold),
    labelSmall = Base.copy(fontSize = 11.sp, fontWeight = FontWeight.Bold),
)

private val colors = lightColorScheme(
    primary = Rose,
    onPrimary = Color.White,
    primaryContainer = Blush,
    onPrimaryContainer = Wine,
    secondary = Wine,
    background = Canvas,
    onBackground = Ink,
    surface = Color.White,
    onSurface = Ink,
    error = RosePressed,
    onError = Color.White,
)

val LocalFontStep = staticCompositionLocalOf { 0 }

@Composable
fun VortexTheme(fontStep: Int, content: @Composable () -> Unit) {
    val scale = listOf(15f, 17f, 19f)[fontStep.coerceIn(0, 2)] / 15f
    val current = LocalDensity.current
    CompositionLocalProvider(
        LocalDensity provides Density(current.density, current.fontScale * scale),
        LocalContentColor provides Ink,
        LocalFontStep provides fontStep,
    ) {
        MaterialTheme(colorScheme = colors, typography = VortexTypography, content = content)
    }
}
