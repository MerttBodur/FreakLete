// FreakLete Shared Components

// ── Icons ──────────────────────────────────────────────────
const IconHome = ({color='currentColor',size=22}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M3 9.5L12 3l9 6.5V20a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V9.5z"/><path d="M9 21V12h6v9"/>
  </svg>
);
const IconDumbbell = ({color='currentColor',size=22}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M6.5 6.5h11M6.5 17.5h11"/><rect x="2" y="5" width="3" height="14" rx="1"/><rect x="19" y="5" width="3" height="14" rx="1"/><line x1="8" y1="12" x2="16" y2="12"/>
  </svg>
);
const IconCalc = ({color='currentColor',size=22}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="4" y="2" width="16" height="20" rx="2"/><line x1="8" y1="6" x2="16" y2="6"/><line x1="8" y1="10" x2="16" y2="10"/><line x1="8" y1="14" x2="14" y2="14"/>
  </svg>
);
const IconProfile = ({color='currentColor',size=22}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/>
  </svg>
);
const IconAI = ({color='currentColor',size=22}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M420 98 L384 174"/><circle cx="12" cy="12" r="9"/><path d="M9 12l2 2 4-4"/>
  </svg>
);
const IconChevronRight = ({color='currentColor',size=16}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="9 18 15 12 9 6"/>
  </svg>
);
const IconPlus = ({color='currentColor',size=18}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round">
    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
  </svg>
);
const IconBolt = ({color='#A78BFA',size=20}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill={color} stroke="none">
    <path d="M13 2L4.5 13.5H11L10 22L19.5 10H13L13 2Z"/>
  </svg>
);
const IconCalendar = ({color='currentColor',size=18}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
  </svg>
);
const IconCheck = ({color='currentColor',size=16}) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12"/>
  </svg>
);

// ── TopBar ─────────────────────────────────────────────────
const TopBar = ({ title, right }) => (
  <div style={{
    background: T.topBar, padding: '14px 20px',
    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
    borderBottom: `1px solid ${T.borderSubtle}`, minHeight: 56,
  }}>
    <span style={{ fontFamily: T.font, fontSize: 21, fontWeight: 600, color: T.textPrimary }}>{title}</span>
    {right && <div>{right}</div>}
  </div>
);

// ── BottomNav ──────────────────────────────────────────────
const NAV_ITEMS = [
  { id: 'home',     label: 'Home',    Icon: IconHome },
  { id: 'workout',  label: 'Workout', Icon: IconDumbbell },
  { id: 'calc',     label: 'Calc',    Icon: IconCalc },
  { id: 'profile',  label: 'Profile', Icon: IconProfile },
];

const BottomNav = ({ active, onNav }) => (
  <div style={{
    padding: '8px 12px 12px',
    background: T.background,
  }}>
    <div style={{
      background: T.surface, border: `1px solid ${T.border}`,
      borderRadius: T.radiusShell, boxShadow: '0 4px 20px rgba(0,0,0,0.35)',
      display: 'flex', padding: '6px 8px',
    }}>
      {NAV_ITEMS.map(({ id, label, Icon }) => {
        const isActive = active === id;
        return (
          <div key={id} onClick={() => onNav(id)} style={{
            flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center',
            gap: 3, padding: '7px 4px', borderRadius: T.radiusBase, cursor: 'pointer',
            background: isActive ? T.accentSoft : 'transparent',
            transition: 'background 0.15s',
          }}>
            <Icon color={isActive ? T.accentGlow : T.textMuted} />
            <span style={{ fontSize: 10, fontWeight: 600, fontFamily: T.font, color: isActive ? T.accentGlow : T.textMuted }}>{label}</span>
          </div>
        );
      })}
    </div>
  </div>
);

// ── Card ───────────────────────────────────────────────────
const Card = ({ children, variant='standard', style={} }) => {
  const bg = variant === 'elevated' ? T.gradientCard : variant === 'accent' ? T.gradientAccent : T.surface;
  const borderColor = variant === 'accent' ? T.accent : T.border;
  return (
    <div style={{
      background: bg, border: `1px solid ${borderColor}`,
      borderRadius: T.radiusCard, padding: 18, ...style,
    }}>
      {children}
    </div>
  );
};

// ── MetricTile ─────────────────────────────────────────────
const MetricTile = ({ label, value, unit, color }) => (
  <div style={{
    background: T.surfaceRaised, border: `1px solid ${T.border}`,
    borderRadius: T.radiusMd, padding: '12px 14px', flex: 1,
  }}>
    <div style={{ fontSize: 10, fontWeight: 600, color: T.textMuted, fontFamily: T.font, textTransform: 'uppercase', letterSpacing: '0.07em', marginBottom: 6 }}>{label}</div>
    <div style={{ fontSize: 20, fontWeight: 600, color: color || T.accentGlow, fontFamily: T.font, lineHeight: 1 }}>{value}</div>
    {unit && <div style={{ fontSize: 11, color: T.textMuted, fontFamily: T.font, marginTop: 3 }}>{unit}</div>}
  </div>
);

// ── Button ─────────────────────────────────────────────────
const Button = ({ children, variant='primary', size='md', onClick, style={} }) => {
  const bg = variant === 'primary' ? T.accent : variant === 'secondary' ? T.surfaceStrong : T.dangerSoft;
  const textColor = variant === 'danger' ? T.danger : T.textPrimary;
  const border = variant === 'secondary' ? `1px solid ${T.border}` : variant === 'danger' ? `1px solid ${T.danger}` : 'none';
  const pad = size === 'sm' ? '9px 16px' : '13px 22px';
  const fs = size === 'sm' ? 13 : 15;
  return (
    <button onClick={onClick} style={{
      background: bg, color: textColor, border, fontFamily: T.font,
      fontSize: fs, fontWeight: 600, borderRadius: T.radiusBase,
      padding: pad, minHeight: size === 'sm' ? 36 : 44, cursor: 'pointer',
      ...style,
    }}>{children}</button>
  );
};

// ── Eyebrow ────────────────────────────────────────────────
const Eyebrow = ({ children }) => (
  <div style={{ fontSize: 11, fontWeight: 600, color: T.textMuted, fontFamily: T.font, textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 6 }}>
    {children}
  </div>
);

// ── Badge ──────────────────────────────────────────────────
const Badge = ({ children, color=T.accentGlow, bg=T.accentSoft }) => (
  <span style={{
    background: bg, color, fontFamily: T.font,
    fontSize: 11, fontWeight: 600, borderRadius: 20,
    padding: '3px 10px', display: 'inline-block',
  }}>{children}</span>
);

// ── Divider ────────────────────────────────────────────────
const Divider = () => <div style={{ height: 1, background: T.borderSubtle, margin: '4px 0' }} />;

// ── Input ──────────────────────────────────────────────────
const Input = ({ placeholder, value, onChange, label }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
    {label && <div style={{ fontSize: 12, fontWeight: 600, color: T.textMuted, fontFamily: T.font, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{label}</div>}
    <input
      placeholder={placeholder} value={value} onChange={onChange}
      style={{
        background: T.surfaceRaised, color: T.textPrimary,
        fontFamily: T.font, fontSize: 15, fontWeight: 600,
        border: `1px solid ${T.border}`, borderRadius: T.radiusBase,
        padding: '13px 16px', minHeight: 48, outline: 'none', width: '100%', boxSizing: 'border-box',
      }}
    />
  </div>
);

// ── ListRow ────────────────────────────────────────────────
const ListRow = ({ icon, title, subtitle, right, onClick }) => (
  <div onClick={onClick} style={{
    display: 'flex', alignItems: 'center', gap: 14, padding: '13px 0', cursor: onClick ? 'pointer' : 'default',
    borderBottom: `1px solid ${T.borderSubtle}`,
  }}>
    {icon && <div style={{ color: T.accentGlow, flexShrink: 0 }}>{icon}</div>}
    <div style={{ flex: 1, minWidth: 0 }}>
      <div style={{ fontSize: 15, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>{title}</div>
      {subtitle && <div style={{ fontSize: 12, color: T.textMuted, fontFamily: T.font, marginTop: 2 }}>{subtitle}</div>}
    </div>
    {right || <IconChevronRight color={T.textMuted} />}
  </div>
);

Object.assign(window, {
  T,
  IconHome, IconDumbbell, IconCalc, IconProfile, IconAI,
  IconChevronRight, IconPlus, IconBolt, IconCalendar, IconCheck,
  TopBar, BottomNav, Card, MetricTile, Button, Eyebrow, Badge, Divider, Input, ListRow,
});
