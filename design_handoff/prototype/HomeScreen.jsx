// HomeScreen — Dashboard with hero card, quick actions, FreakAI entry
const HomeScreen = () => {
  const [aiInput, setAiInput] = React.useState('');

  return (
    <div style={{ flex: 1, overflowY: 'auto', padding: '20px 20px 0' }}>
      {/* Hero — Today's overview */}
      <Card variant="elevated" style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 14 }}>
          <div>
            <Eyebrow>Today</Eyebrow>
            <div style={{ fontSize: 21, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>Ready to train?</div>
          </div>
          <div style={{
            background: T.accentSoft, border: `1px solid ${T.accent}`,
            borderRadius: 12, padding: '6px 12px',
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <IconBolt size={14} />
            <span style={{ fontSize: 12, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>Premium</span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 10, marginBottom: 16 }}>
          <MetricTile label="This Week" value="4" unit="sessions" />
          <MetricTile label="Last 1RM" value="145" unit="kg · Squat" />
          <MetricTile label="Streak" value="12" unit="days" color={T.success} />
        </div>
        <Button style={{ width: '100%' }}>
          <span style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}>
            <IconDumbbell color={T.textPrimary} size={18} /> Start Workout
          </span>
        </Button>
      </Card>

      {/* Quick Actions */}
      <div style={{ marginBottom: 16 }}>
        <div style={{ fontSize: 15, fontWeight: 600, color: T.textPrimary, fontFamily: T.font, marginBottom: 10 }}>Quick Access</div>
        <div style={{ display: 'flex', gap: 10 }}>
          {[
            { label: 'Calculations', sub: '1RM · RSI · FFMI', icon: <IconCalc color={T.accentGlow} size={20}/> },
            { label: 'Calendar', sub: 'Workout history', icon: <IconCalendar color={T.accentGlow} size={20}/> },
          ].map(item => (
            <div key={item.label} style={{
              flex: 1, background: T.surfaceRaised, border: `1px solid ${T.border}`,
              borderRadius: T.radiusCard, padding: '14px 14px', cursor: 'pointer',
            }}>
              <div style={{ marginBottom: 8 }}>{item.icon}</div>
              <div style={{ fontSize: 13, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>{item.label}</div>
              <div style={{ fontSize: 11, color: T.textMuted, fontFamily: T.font, marginTop: 2 }}>{item.sub}</div>
            </div>
          ))}
        </div>
      </div>

      {/* FreakAI entry */}
      <Card variant="accent" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
          <IconBolt size={18} />
          <div>
            <div style={{ fontSize: 15, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>FreakAI</div>
            <div style={{ fontSize: 11, color: T.textMuted, fontFamily: T.font }}>3 messages remaining today</div>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <input
            value={aiInput}
            onChange={e => setAiInput(e.target.value)}
            placeholder="Ask your coach..."
            style={{
              flex: 1, background: T.surfaceRaised, color: T.textPrimary,
              fontFamily: T.font, fontSize: 14, fontWeight: 400,
              border: `1px solid ${T.border}`, borderRadius: T.radiusBase,
              padding: '11px 14px', outline: 'none', minHeight: 44,
            }}
          />
          <button style={{
            background: T.accent, border: 'none', borderRadius: T.radiusBase,
            width: 44, height: 44, cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
          }}>
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke={T.textPrimary} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>
            </svg>
          </button>
        </div>
      </Card>
    </div>
  );
};

Object.assign(window, { HomeScreen });
