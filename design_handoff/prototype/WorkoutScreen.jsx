// WorkoutScreen — Workout landing with templates & active session
const WorkoutScreen = () => {
  const [view, setView] = React.useState('landing'); // 'landing' | 'active'
  const [sets, setSets] = React.useState([
    { exercise: 'Back Squat', weight: '120', reps: '5', done: false },
    { exercise: 'Romanian Deadlift', weight: '90', reps: '8', done: false },
    { exercise: 'Power Clean', weight: '80', reps: '3', done: false },
  ]);
  const [timer, setTimer] = React.useState(0);

  React.useEffect(() => {
    if (view !== 'active') return;
    const t = setInterval(() => setTimer(s => s + 1), 1000);
    return () => clearInterval(t);
  }, [view]);

  const fmt = s => `${String(Math.floor(s/60)).padStart(2,'0')}:${String(s%60).padStart(2,'0')}`;

  const toggleSet = i => setSets(prev => prev.map((s,j) => j===i ? {...s, done: !s.done} : s));

  const TEMPLATES = [
    { name: 'Power Block A', tags: ['Strength', 'Olympic'], sessions: 3 },
    { name: 'Sprint + Lower', tags: ['Speed', 'Plyometric'], sessions: 4 },
    { name: 'Upper Hypertrophy', tags: ['Hypertrophy', 'Push'], sessions: 3 },
  ];

  if (view === 'active') return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflowY: 'auto' }}>
      {/* Active session header */}
      <div style={{
        background: T.gradientAccent, borderBottom: `1px solid ${T.border}`,
        padding: '16px 20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      }}>
        <div>
          <Eyebrow>Live Session</Eyebrow>
          <div style={{ fontSize: 28, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>{fmt(timer)}</div>
        </div>
        <Button variant="secondary" size="sm" onClick={() => { setView('landing'); setTimer(0); }}>End</Button>
      </div>
      {/* Sets list */}
      <div style={{ padding: '16px 20px', flex: 1 }}>
        {sets.map((s, i) => (
          <div key={i} onClick={() => toggleSet(i)} style={{
            background: s.done ? T.successSoft : T.surfaceRaised,
            border: `1px solid ${s.done ? T.success : T.border}`,
            borderRadius: T.radiusMd, padding: '14px 16px', marginBottom: 10, cursor: 'pointer',
            display: 'flex', alignItems: 'center', gap: 12, transition: 'background 0.2s',
          }}>
            <div style={{
              width: 28, height: 28, borderRadius: '50%', flexShrink: 0,
              background: s.done ? T.success : T.surfaceStrong,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              {s.done && <IconCheck color="#fff" size={14} />}
            </div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 14, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>{s.exercise}</div>
              <div style={{ fontSize: 12, color: T.textMuted, fontFamily: T.font, marginTop: 2 }}>{s.weight} kg × {s.reps} reps</div>
            </div>
          </div>
        ))}
        <button style={{
          width: '100%', background: 'transparent', border: `1px dashed ${T.border}`,
          borderRadius: T.radiusMd, padding: '13px', color: T.textMuted,
          fontFamily: T.font, fontSize: 14, fontWeight: 600, cursor: 'pointer',
          display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8,
        }}>
          <IconPlus color={T.textMuted} /> Add Exercise
        </button>
      </div>
    </div>
  );

  return (
    <div style={{ flex: 1, overflowY: 'auto', padding: '20px 20px 0' }}>
      {/* Start workout CTA */}
      <Card variant="elevated" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
          <div>
            <Eyebrow>Workout</Eyebrow>
            <div style={{ fontSize: 18, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>Start a session</div>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <Button style={{ flex: 1 }} onClick={() => setView('active')}>Quick Start</Button>
          <Button variant="secondary" style={{ flex: 1 }}>From Program</Button>
        </div>
      </Card>

      {/* Starter templates */}
      <div style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <div style={{ fontSize: 15, fontWeight: 600, color: T.textPrimary, fontFamily: T.font }}>Starter Templates</div>
          <span style={{ fontSize: 12, color: T.accentGlow, fontFamily: T.font, fontWeight: 600, cursor: 'pointer' }}>See all</span>
        </div>
        {TEMPLATES.map((t,i) => (
          <Card key={i} style={{ marginBottom: 10, padding: '14px 16px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div style={{ fontSize: 15, fontWeight: 600, color: T.textPrimary, fontFamily: T.font, marginBottom: 6 }}>{t.name}</div>
                <div style={{ display: 'flex', gap: 6 }}>
                  {t.tags.map(tag => <Badge key={tag}>{tag}</Badge>)}
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 18, fontWeight: 600, color: T.accentGlow, fontFamily: T.font }}>{t.sessions}</div>
                <div style={{ fontSize: 10, color: T.textMuted, fontFamily: T.font }}>sessions/wk</div>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};

Object.assign(window, { WorkoutScreen });
