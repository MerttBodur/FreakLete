// ProfileScreen — Body metrics, athletic performance, movement goals
const ProfileScreen = () => {
  const [section, setSection] = React.useState('overview'); // overview | performance | goals

  const SECTIONS = [
    { id: 'overview', label: 'Overview' },
    { id: 'performance', label: 'Performance' },
    { id: 'goals', label: 'Goals' },
  ];

  const PERF_DATA = [
    { movement: 'Vertical Jump', value: '68', unit: 'cm', date: 'Apr 12' },
    { movement: 'Back Squat 1RM', value: '145', unit: 'kg', date: 'Apr 8' },
    { movement: 'Power Clean 1RM', value: '100', unit: 'kg', date: 'Apr 1' },
    { movement: '40y Dash', value: '4.52', unit: 's', date: 'Mar 28' },
  ];

  const GOALS = [
    { movement: 'Vertical Jump', target: '75 cm', current: '68 cm', pct: 0.77 },
    { movement: 'Back Squat', target: '160 kg', current: '145 kg', pct: 0.72 },
    { movement: 'Deadlift', target: '200 kg', current: '175 kg', pct: 0.65 },
  ];

  return (
    <div style={{ flex: 1, overflowY: 'auto' }}>
      {/* Profile hero */}
      <div style={{
        background: T.gradientAccent, borderBottom: `1px solid ${T.border}`,
        padding: '20px 20px 16px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 16 }}>
          <div style={{
            width: 56, height: 56, borderRadius: '50%',
            background: T.accentSoft, border: `2px solid ${T.accent}`,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <span style={{ fontSize: 22, fontWeight: 700, color: T.accentGlow, fontFamily: T.font }}>M</span>
          </div>
          <div>
            <div style={{ fontSize: 18, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>Mert B.</div>
            <div style={{ fontSize: 12, color: T.textMuted, fontFamily: T.font }}>Wide Receiver · Football</div>
            <Badge style={{ marginTop: 4 }}>Premium</Badge>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <MetricTile label="Weight" value="82" unit="kg" />
          <MetricTile label="Body Fat" value="11" unit="%" />
          <MetricTile label="FFMI" value="21.3" unit="normalized" />
        </div>
      </div>

      {/* Section tabs */}
      <div style={{
        display: 'flex', background: T.surface, borderBottom: `1px solid ${T.border}`,
        padding: '8px 20px', gap: 4,
      }}>
        {SECTIONS.map(s => (
          <div key={s.id} onClick={() => setSection(s.id)} style={{
            padding: '8px 16px', borderRadius: T.radiusMd, cursor: 'pointer',
            background: section === s.id ? T.accentSoft : 'transparent',
            fontFamily: T.font, fontSize: 13, fontWeight: 600,
            color: section === s.id ? T.accentGlow : T.textMuted,
            transition: 'background 0.15s',
          }}>{s.label}</div>
        ))}
      </div>

      <div style={{ padding: '20px 20px 0' }}>
        {/* Overview */}
        {section === 'overview' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <Card>
              <Eyebrow>Sport Profile</Eyebrow>
              <ListRow title="Sport" subtitle="American Football" right={<span style={{ fontSize: 13, color: T.textMuted, fontFamily: T.font }}>WR</span>} />
              <ListRow title="Training focus" subtitle="Speed · Explosiveness · Strength" right={<span/>} />
              <ListRow title="Coach" subtitle="Self-coached" right={<span/>} />
            </Card>
            <Card>
              <Eyebrow>Body Metrics</Eyebrow>
              <ListRow title="Weight" subtitle="Last updated Apr 14" right={<span style={{ fontSize: 15, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>82 kg</span>} />
              <ListRow title="Height" subtitle="Profile" right={<span style={{ fontSize: 15, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>183 cm</span>} />
              <ListRow title="Body Fat" subtitle="Estimated" right={<span style={{ fontSize: 15, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>11%</span>} />
            </Card>
          </div>
        )}

        {/* Athletic Performance */}
        {section === 'performance' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 4 }}>
              <Button variant="secondary" size="sm"><span style={{ display: 'flex', alignItems: 'center', gap: 6 }}><IconPlus size={14}/> Log Result</span></Button>
            </div>
            {PERF_DATA.map((p, i) => (
              <div key={i} style={{
                background: T.surfaceRaised, border: `1px solid ${T.border}`,
                borderRadius: T.radiusMd, padding: '14px 16px',
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>{p.movement}</div>
                  <div style={{ fontSize: 11, color: T.textMuted, fontFamily: T.font, marginTop: 2 }}>{p.date}</div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <span style={{ fontSize: 22, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>{p.value}</span>
                  <span style={{ fontSize: 13, color: T.textMuted, fontFamily: T.font, marginLeft: 4 }}>{p.unit}</span>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Movement Goals */}
        {section === 'goals' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            {GOALS.map((g, i) => (
              <Card key={i}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 10 }}>
                  <div style={{ fontSize: 14, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>{g.movement}</div>
                  <Badge>{Math.round(g.pct * 100)}%</Badge>
                </div>
                <div style={{ height: 6, background: T.surfaceStrong, borderRadius: 99, marginBottom: 8, overflow: 'hidden' }}>
                  <div style={{ height: '100%', width: `${g.pct * 100}%`, background: T.accent, borderRadius: 99, transition: 'width 0.5s ease' }} />
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ fontSize: 11, color: T.textMuted, fontFamily: T.font }}>Current: {g.current}</span>
                  <span style={{ fontSize: 11, color: T.textSecondary, fontFamily: T.font }}>Target: {g.target}</span>
                </div>
              </Card>
            ))}
            <button style={{
              background: 'transparent', border: `1px dashed ${T.border}`,
              borderRadius: T.radiusCard, padding: '14px', color: T.textMuted,
              fontFamily: T.font, fontSize: 14, fontWeight: 600, cursor: 'pointer',
              display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8,
            }}>
              <IconPlus color={T.textMuted} /> Add Movement Goal
            </button>
          </div>
        )}
        <div style={{ height: 20 }} />
      </div>
    </div>
  );
};

Object.assign(window, { ProfileScreen });
