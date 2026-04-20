// FreakLete Design Tokens — JS side
const T = {
  // Colors
  background:    '#100D1A',
  surface:       '#171321',
  surfaceRaised: '#1D1828',
  surfaceStrong: '#251F33',
  topBar:        '#161125',
  accent:        '#8B5CF6',
  accentGlow:    '#A78BFA',
  accentSoft:    '#2F2346',
  border:        '#342D46',
  borderSubtle:  '#2A2437',
  textPrimary:   '#F7F7FB',
  textSecondary: '#B3B2C5',
  textMuted:     '#8A889B',
  success:       '#22C55E',
  successSoft:   '#0D2818',
  warning:       '#F59E0B',
  warningSoft:   '#2A1F06',
  danger:        '#DC2626',
  dangerSoft:    '#3A1623',
  error:         '#EF4444',
  info:          '#3B82F6',
  infoSoft:      '#0D1B2A',

  // Gradients
  gradientCard:   'linear-gradient(160deg, #1D1828 0%, #171321 100%)',
  gradientAccent: 'linear-gradient(160deg, #2F2346 0%, #171321 100%)',

  // Radius
  radiusSm:    10,
  radiusMd:    14,
  radiusBase:  18,
  radiusCard:  24,
  radiusShell: 26,

  // Font
  font: "'Open Sans', sans-serif",
};

Object.assign(window, { T });
