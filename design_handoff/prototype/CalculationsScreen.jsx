// CalculationsScreen — 1RM, RSI, FFMI tools
const CalculationsScreen = () => {
  const [tab, setTab] = React.useState('1rm');
  // 1RM state
  const [weight, setWeight] = React.useState('');
  const [reps, setReps] = React.useState('');
  const [result1rm, setResult1rm] = React.useState(null);
  // RSI state
  const [jumpH, setJumpH] = React.useState('');
  const [gct, setGct] = React.useState('');
  const [resultRsi, setResultRsi] = React.useState(null);

  const calc1rm = () => {
    const w = parseFloat(weight), r = parseInt(reps);
    if (!w || !r) return;
    const val = w * (1 + r / 30);
    setResult1rm(val.toFixed(1));
  };

  const calcRsi = () => {
    const h = parseFloat(jumpH) / 100, g = parseFloat(gct) / 1000;
    if (!h || !g) return;
    const airTime = Math.sqrt(2 * h / 9.81);
    setResultRsi((airTime / g).toFixed(2));
  };

  const TABS = [
    { id: '1rm', label: '1RM' },
    { id: 'rsi', label: 'RSI' },
    { id: 'ffmi', label: 'FFMI' },
  ];

  return (
    <div style={{ flex: 1, overflowY: 'auto', padding: '20px 20px 0' }}>
      {/* Tab switcher */}
      <div style={{
        display: 'flex', background: T.surfaceRaised, border: `1px solid ${T.border}`,
        borderRadius: T.radiusBase, padding: 4, marginBottom: 20, gap: 4,
      }}>
        {TABS.map(t => (
          <div key={t.id} onClick={() => { setTab(t.id); setResult1rm(null); setResultRsi(null); }} style={{
            flex: 1, textAlign: 'center', padding: '9px 0',
            borderRadius: 14, cursor: 'pointer',
            background: tab === t.id ? T.accent : 'transparent',
            fontFamily: T.font, fontSize: 14, fontWeight: 600,
            color: tab === t.id ? T.textPrimary : T.textMuted,
            transition: 'background 0.15s',
          }}>{t.label}</div>
        ))}
      </div>

      {/* 1RM */}
      {tab === '1rm' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <Card variant="elevated">
            <Eyebrow>1 Rep Max Estimator</Eyebrow>
            <div style={{ fontSize: 13, color: T.textSecondary, fontFamily: T.font, marginBottom: 16, lineHeight: 1.5 }}>
              Enter a weight and reps to estimate your 1RM using the Epley formula.
            </div>
            <div style={{ display: 'flex', gap: 10, marginBottom: 14 }}>
              <div style={{ flex: 1 }}>
                <Input label="Weight (kg)" placeholder="120" value={weight} onChange={e => setWeight(e.target.value)} />
              </div>
              <div style={{ flex: 1 }}>
                <Input label="Reps" placeholder="5" value={reps} onChange={e => setReps(e.target.value)} />
              </div>
            </div>
            <Button style={{ width: '100%' }} onClick={calc1rm}>Calculate</Button>
          </Card>
          {result1rm && (
            <Card variant="accent">
              <Eyebrow>Estimated 1RM</Eyebrow>
              <div style={{ fontSize: 42, fontWeight: 600, color: T.accentGlow, fontFamily: T.font, lineHeight: 1 }}>{result1rm} <span style={{ fontSize: 18 }}>kg</span></div>
              <Divider />
              <div style={{ display: 'flex', gap: 10, marginTop: 12 }}>
                {[[0.9,'90%'],[0.8,'80%'],[0.7,'70%']].map(([pct,lbl]) => (
                  <MetricTile key={lbl} label={lbl} value={(result1rm * pct).toFixed(1)} unit="kg" />
                ))}
              </div>
            </Card>
          )}
        </div>
      )}

      {/* RSI */}
      {tab === 'rsi' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <Card variant="elevated">
            <Eyebrow>Reactive Strength Index</Eyebrow>
            <div style={{ fontSize: 13, color: T.textSecondary, fontFamily: T.font, marginBottom: 16, lineHeight: 1.5 }}>
              RSI = air time ÷ ground contact time. Measures reactive strength in plyometric movements.
            </div>
            <div style={{ display: 'flex', gap: 10, marginBottom: 14 }}>
              <div style={{ flex: 1 }}>
                <Input label="Jump Height (cm)" placeholder="45" value={jumpH} onChange={e => setJumpH(e.target.value)} />
              </div>
              <div style={{ flex: 1 }}>
                <Input label="GCT (ms)" placeholder="200" value={gct} onChange={e => setGct(e.target.value)} />
              </div>
            </div>
            <Button style={{ width: '100%' }} onClick={calcRsi}>Calculate</Button>
          </Card>
          {resultRsi && (
            <Card variant="accent">
              <Eyebrow>RSI Score</Eyebrow>
              <div style={{ fontSize: 42, fontWeight: 600, color: T.accentGlow, fontFamily: T.font, lineHeight: 1 }}>{resultRsi}</div>
              <div style={{ fontSize: 12, color: T.textMuted, fontFamily: T.font, marginTop: 6 }}>
                {resultRsi >= 2.5 ? 'Advanced reactive ability' : resultRsi >= 1.5 ? 'Intermediate reactive ability' : 'Developing reactive ability'}
              </div>
            </Card>
          )}
        </div>
      )}

      {/* FFMI */}
      {tab === 'ffmi' && (
        <Card variant="elevated">
          <Eyebrow>Fat-Free Mass Index</Eyebrow>
          <div style={{ fontSize: 13, color: T.textSecondary, fontFamily: T.font, marginBottom: 16, lineHeight: 1.5 }}>
            FFMI requires weight, height, and body-fat % from your profile.
          </div>
          <div style={{
            background: T.accentSoft, border: `1px solid ${T.border}`,
            borderRadius: T.radiusMd, padding: '14px 16px', textAlign: 'center',
          }}>
            <div style={{ fontSize: 13, color: T.textSecondary, fontFamily: T.font, marginBottom: 12 }}>
              Complete your profile to unlock FFMI calculation
            </div>
            <Button variant="secondary" size="sm">Go to Profile</Button>
          </div>
        </Card>
      )}

      <div style={{ height: 20 }} />
    </div>
  );
};

Object.assign(window, { CalculationsScreen });
