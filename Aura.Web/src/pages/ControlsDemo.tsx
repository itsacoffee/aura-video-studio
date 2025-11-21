import React, { useState } from 'react';

/**
 * Demo page for the new Aura controls system
 * This page showcases all button variants and form controls
 */
const ControlsDemo: React.FC = () => {
  const [toggleChecked, setToggleChecked] = useState(false);
  const [sliderValue, setSliderValue] = useState(50);
  const [inputValue, setInputValue] = useState('');
  const [selectValue, setSelectValue] = useState('option1');

  return (
    <div style={{ padding: '48px', maxWidth: '1200px', margin: '0 auto' }}>
      <h1>Aura Controls Demo</h1>
      <p style={{ color: 'var(--color-text-secondary)', marginBottom: '32px' }}>
        Showcasing enhanced button and control styles with micro-interactions
      </p>

      {/* Button Variants Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Button Variants</h2>
        <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap', marginTop: '16px' }}>
          <button className="aura-btn aura-btn--primary">Primary Button</button>
          <button className="aura-btn aura-btn--secondary">Secondary Button</button>
          <button className="aura-btn aura-btn--ghost">Ghost Button</button>
          <button className="aura-btn aura-btn--danger">Danger Button</button>
          <button className="aura-btn aura-btn--success">Success Button</button>
          <button className="aura-btn aura-btn--primary" disabled>
            Disabled Button
          </button>
        </div>
      </section>

      {/* Button Sizes Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Button Sizes</h2>
        <div style={{ display: 'flex', gap: '16px', alignItems: 'center', marginTop: '16px' }}>
          <button className="aura-btn aura-btn--primary aura-btn--small">Small</button>
          <button className="aura-btn aura-btn--primary">Default</button>
          <button className="aura-btn aura-btn--primary aura-btn--large">Large</button>
        </div>
      </section>

      {/* Icon Buttons Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Icon Buttons</h2>
        <div style={{ display: 'flex', gap: '16px', alignItems: 'center', marginTop: '16px' }}>
          <button className="aura-btn aura-btn--primary aura-btn--icon aura-btn--small">
            <span style={{ fontSize: '14px' }}>+</span>
          </button>
          <button className="aura-btn aura-btn--primary aura-btn--icon">
            <span style={{ fontSize: '16px' }}>⚙</span>
          </button>
          <button className="aura-btn aura-btn--primary aura-btn--icon aura-btn--large">
            <span style={{ fontSize: '20px' }}>✓</span>
          </button>
        </div>
      </section>

      {/* Button Group Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Button Group</h2>
        <div className="aura-btn-group" style={{ marginTop: '16px' }}>
          <button className="aura-btn aura-btn--secondary">Left</button>
          <button className="aura-btn aura-btn--secondary">Center</button>
          <button className="aura-btn aura-btn--secondary">Right</button>
        </div>
      </section>

      {/* Input Fields Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Input Fields</h2>
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            gap: '16px',
            marginTop: '16px',
            maxWidth: '400px',
          }}
        >
          <div>
            <label
              htmlFor="default-input"
              style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}
            >
              Default Input
            </label>
            <input
              id="default-input"
              className="aura-input"
              type="text"
              placeholder="Enter text here..."
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
            />
          </div>
          <div>
            <label
              htmlFor="error-input"
              style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}
            >
              Error State
            </label>
            <input
              id="error-input"
              className="aura-input aura-input--error"
              type="text"
              placeholder="Error state"
            />
          </div>
          <div>
            <label
              htmlFor="success-input"
              style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}
            >
              Success State
            </label>
            <input
              id="success-input"
              className="aura-input aura-input--success"
              type="text"
              placeholder="Success state"
            />
          </div>
          <div>
            <label
              htmlFor="disabled-input"
              style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}
            >
              Disabled Input
            </label>
            <input
              id="disabled-input"
              className="aura-input"
              type="text"
              placeholder="Disabled"
              disabled
            />
          </div>
        </div>
      </section>

      {/* Select Dropdown Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Select Dropdown</h2>
        <div style={{ marginTop: '16px', maxWidth: '400px' }}>
          <label
            htmlFor="demo-select"
            style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}
          >
            Choose an option
          </label>
          <div className="aura-select">
            <select
              id="demo-select"
              value={selectValue}
              onChange={(e) => setSelectValue(e.target.value)}
            >
              <option value="option1">Option 1</option>
              <option value="option2">Option 2</option>
              <option value="option3">Option 3</option>
              <option value="option4">Option 4</option>
            </select>
          </div>
        </div>
      </section>

      {/* Toggle Switch Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Toggle Switch</h2>
        <div style={{ marginTop: '16px' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '12px', cursor: 'pointer' }}>
            <div className="aura-toggle">
              <input
                type="checkbox"
                checked={toggleChecked}
                onChange={(e) => setToggleChecked(e.target.checked)}
              />
              <span className="aura-toggle__slider"></span>
            </div>
            <span style={{ fontSize: 'var(--font-size-sm)' }}>
              Toggle is {toggleChecked ? 'ON' : 'OFF'}
            </span>
          </label>
        </div>
      </section>

      {/* Range Slider Section */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Range Slider</h2>
        <div style={{ marginTop: '16px', maxWidth: '400px' }}>
          <label style={{ display: 'block', marginBottom: '8px', fontSize: 'var(--font-size-sm)' }}>
            Value: {sliderValue}
          </label>
          <div className="aura-slider">
            <input
              type="range"
              min="0"
              max="100"
              value={sliderValue}
              onChange={(e) => setSliderValue(Number(e.target.value))}
            />
          </div>
        </div>
      </section>

      {/* Interactive Examples */}
      <section style={{ marginBottom: '48px' }}>
        <h2>Interactive Examples</h2>
        <p style={{ color: 'var(--color-text-secondary)', marginBottom: '16px' }}>
          Try clicking buttons to see the ripple effect
        </p>
        <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
          <button className="aura-btn aura-btn--primary" onClick={() => alert('Primary clicked!')}>
            Click for Ripple
          </button>
          <button className="aura-btn aura-btn--success" onClick={() => alert('Success!')}>
            Success Action
          </button>
          <button className="aura-btn aura-btn--danger" onClick={() => alert('Danger!')}>
            Danger Action
          </button>
        </div>
      </section>
    </div>
  );
};

export default ControlsDemo;
