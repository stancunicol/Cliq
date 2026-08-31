import { Activity, Check, CirclePlay, Plus, Search, X } from 'lucide-react'
import type { ProjectManifest, TestCase } from '../../types'
import cliqMark from '../../assets/cliq-mark.svg'

interface SidebarProps {
  project: ProjectManifest
  tests: TestCase[]
  selectedId?: string
  active: 'tests' | 'runs'
  search: string
  onSearch(value: string): void
  onSelect(id: string): void
  onCreate(): void
  onActive(value: 'tests' | 'runs'): void
}

export function Sidebar(props: SidebarProps) {
  const filtered = props.tests.filter((test) =>
    `${test.name} ${test.tags.join(' ')}`.toLowerCase().includes(props.search.toLowerCase()),
  )
  return (
    <aside className="sidebar">
      <div className="titlebar-drag" />
      <div className="brand"><img className="logo" src={cliqMark} alt="" /><strong>Cliq</strong></div>
      <nav className="primary-nav">
        <button className={props.active === 'tests' ? 'active' : ''} onClick={() => props.onActive('tests')}>
          <Activity />Tests <span>{props.tests.length}</span>
        </button>
        <button className={props.active === 'runs' ? 'active' : ''} onClick={() => props.onActive('runs')}>
          <CirclePlay />Runs
        </button>
      </nav>
      <div className="sidebar-library">
        <div className="section-title">
          <span>{props.project.name}</span>
          <button aria-label="Create test" onClick={props.onCreate}><Plus /></button>
        </div>
        <div className="search sidebar-search">
          <Search />
          <input
            aria-label="Search tests"
            value={props.search}
            onChange={(event) => props.onSearch(event.target.value)}
            placeholder="Search tests"
          />
        </div>
        <div className="sidebar-tests">
          {filtered.map((test) => (
            <button
              key={test.id}
              className={test.id === props.selectedId ? 'test active' : 'test'}
              onClick={() => props.onSelect(test.id)}
            >
              <span className="status draft">{test.steps.length > 0 ? <Check /> : <span />}</span>
              <span>
                <strong>{test.name}</strong>
                <small>{test.steps.length} steps{test.tags[0] ? ` · ${test.tags[0]}` : ''}</small>
              </span>
            </button>
          ))}
          {filtered.length === 0 && <div className="sidebar-empty"><X />No matching tests</div>}
        </div>
      </div>
      <div className="sidebar-bottom">
        <div className="profile">
          <div>CL</div>
          <span><strong>Local workspace</strong><small>{props.project.defaultBrowser}</small></span>
          <span className="online" />
        </div>
      </div>
    </aside>
  )
}
