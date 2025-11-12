/**
 * Icon Fallbacks Module
 * Provides base64-encoded fallback icons for when icon files are not available
 * This ensures the app can always display icons, even if asset loading fails
 */

/**
 * Simple 16x16 purple gradient icon (base64 encoded PNG)
 * This is a minimal icon that represents the Aura brand colors
 */
const FALLBACK_ICON_16_BASE64 = 
  'iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAAsTAAALEwEAmpwYAAAA' +
  'AXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAACMSURBVHgBrZMxCoAwDEX/V/QYHsObeBMvIPgp' +
  'nkWHghZ0UBBBBYX+CbaBQpJSB/sghPDevCRNCLKqqhARAcZYuO6O67oQEYwxcM5hjAHnnFJKqaoo' +
  'pYQxBkIIRVVVqKqCUgpCCFRVBUopKKVgjIGUEkopEBEIIVBVVVBKQSkFKSWcc1BKQQiBiAiI/8gn' +
  'qRt39w4x/gAAAABJRU5ErkJggg==';

/**
 * Simple 32x32 purple gradient icon (base64 encoded PNG)
 */
const FALLBACK_ICON_32_BASE64 = 
  'iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAACXBIWXMAABJ0AAASdAHeZh94AAAA' +
  'AXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAE8SURBVHgB7ZdBCsIwEEX/TNCDeBNv4k08QGdp' +
  'L+LBBBdKhRZBEAQVBVFQUXBTBe2iDdYuUoxJM5OkKv1LSJp08vMnmUwI+VeMRiOMx2NMJhNMp1PM' +
  'ZjMsF0ssliusl2tslhts11vs9nvs9gdsDkdsdkfkZRlvt0dVVXh5veCyv+Dj84NP9Ym3zxf2+wMu' +
  'lwuqqqKqqvB6vVBVFV7fX3i9XfH8eMLT4xkPD4+4u7/H7d0dbu/ukGUZyrJEWZYoy7JhY2NDQ5Ik' +
  'SZIkiKIIYRgiCAIEQQC/2cD3fXieB8/z4Lou3FYHF8cxoihCGIYIggBhGCKKIiRJgiRJkCRJ0+t2' +
  'u816mqZIkgSv1wuX8xn73Q6bzQbr1QqrxQKLxQLz+Ryz2Qyz2RxZliHPc+R5jjzPUZYl8jxHURQo' +
  'igJFUaAoCnx9fTX7+fn5g/wJfAMrPFSQ+3rLPwAAAABJRU5ErkJggg==';

/**
 * Simple 256x256 purple gradient icon (base64 encoded PNG)
 * This is used for larger displays and can be scaled down
 */
const FALLBACK_ICON_256_BASE64 = 
  'iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAAACXBIWXMAAAsTAAALEwEAmpwYAAAA' +
  'AXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAARdSURBVHgB7d1NctpAFAbQlmyIPciZsgXPrMGz' +
  'Z9aQyQ6ylWQDmewga8hkB96CZ+6ArCGTLXjmHiqWQgJJIP04Ff2dc1IxWJKr6Fevu1sBAAAAAAAA' +
  'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA' +
  'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA' +
  'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAOzfn/sA7MbpdDrcxz7A/7wGeBOAXQT4fYBbAW4FaAX4HYBW' +
  'gFsB2hfgVYBbAWoFuBPgToA3AdoX4FWANwFqBfgWoA0F6AS4F+BegPcBvgf4EeBHgJ8BfgX4HeBP' +
  'gL8B/gX4H+BfgJdf4OWXP/kV/i8/w2fw/d/h83j7/F//LX+H14cv/N/xdfxfxx/w/P9/hc/j/f7f' +
  '8Dm8ff7P8Lm8ff7P8Hl8//xfw+fy/Pm/hs/l+fN/DZ/L8+f/Gj6X58//NXwuz5//a/hcnj//1/C5' +
  'PH/+r+Fzef78X8Pn8vz5v4bP5fnzfw2fy/Pn/xo+l+fP/zV8Ls+f/2v4XJ4//9fwuTx//q/hc3n+' +
  '/F/D5/L8+b+Gz+X583+Nz+f5838Nn8vz5/8aPs/z5/8aPs/z5/8aPsfz5/8aPs/z5/8aPsfz5/8a' +
  'Psfz5/8aPsfz5/8aPs/z5/8aPsfz5/8aPs/z5/8aPs/z5/8aPs/z5/8aPs/z5/8an+f583+Nz/P8' +
  '+b/G53n+/F/j8zx//q/xeZ4//9f4PM+f/2t8nufP/zU+z/Pn/xqf5/nzf43P8/z5v8bned78X+Nz' +
  'PW/+r/G5njf/1/hcz5v/a3yu583/NT7X8+b/Gp/refN/jc/1vPm/xud63vxf43M9b/6v8bmeN//X' +
  '+FzPm/9rfK7nzf81Ptfz5v8an+t583+Nz/W8+b/G53re/F/jcz1v/q/xuZ43/9f4XM+b/2t8rufN' +
  '/zU+1/Pm/xqf63nzf43P9bz5v8bnev78X+PzPH/+r/F5nj//1/g8z5//a3ye58//NT7P8+f/Gp/n' +
  '+fN/jc/z/Pm/xud5/vxf4/M8f/6v8XmeP//X+DzPn/9rfJ7nz/81Ps/z5/8an+f583+Nz/P8+b/G' +
  '53n+/F/j8zx//q/xeZ4//9f4PM+f/2t8nufP/zU+z/Pn/xqf5/nzf43P8/z5v8bned78X+NzPW/+' +
  'r/G5njf/1/hcz5v/a3yu583/NT7X8+b/Gp/refN/jc/1vPm/xud63vxf43M9b/6v8bmeN//X+FzP' +
  'm/9rfK7nzf81Ptfz5v8an+t583+Nz/W8+b/G53re/F/jcz1v/q/xud78X+PzPW/+r/H5njf/1/h8' +
  'z5v/a3y+583/NT7f8+b/Gp/vefN/jc/3vPm/xud73vxf4/M9b/6v8fmeN//X+HzPm/9rfL7nzf81' +
  'Pt/z5v8an+9583+Nz/e8+b/G53ve/F/j8z1v/q/x+Z43/9f4fM+b/2t8vufN/7V9+xvgB4CBAE0A' +
  '7gPcBfgW4DZAG+BbgNsAbYA2QBugDdAG6P0/wL0AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA' +
  'AAAAAAAAAAAAAAAAAAAAAAB28h+vD4XhVxKkjAAAAABJRU5ErkJggg==';

/**
 * Get a fallback icon as a NativeImage
 * @param {Object} nativeImage - Electron's nativeImage module
 * @param {string} size - Icon size: '16', '32', or '256' 
 * @returns {Electron.NativeImage} A NativeImage object
 */
function getFallbackIcon(nativeImage, size = '32') {
  let base64Data;
  
  switch(size) {
    case '16':
      base64Data = FALLBACK_ICON_16_BASE64;
      break;
    case '32':
      base64Data = FALLBACK_ICON_32_BASE64;
      break;
    case '256':
      base64Data = FALLBACK_ICON_256_BASE64;
      break;
    default:
      base64Data = FALLBACK_ICON_32_BASE64;
  }
  
  try {
    return nativeImage.createFromDataURL(`data:image/png;base64,${base64Data}`);
  } catch (error) {
    console.error('Failed to create fallback icon:', error);
    return nativeImage.createEmpty();
  }
}

module.exports = {
  getFallbackIcon,
  FALLBACK_ICON_16_BASE64,
  FALLBACK_ICON_32_BASE64,
  FALLBACK_ICON_256_BASE64
};
