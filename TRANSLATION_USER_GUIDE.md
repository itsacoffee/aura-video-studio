# Translation and Localization User Guide

## Overview

Aura Video Studio provides advanced AI-powered translation with cultural adaptation for 55+ languages. The translation feature goes beyond simple word-for-word translation to adapt content for cultural relevance, ensuring your videos resonate with global audiences.

## Accessing Translation Features

Navigate to **Localization** from the main menu to access the translation interface.

## Features

### 1. Single Translation

Translate content with comprehensive quality assurance and cultural adaptation.

#### How to Use

1. **Select Languages**
   - Choose source language from 55+ supported languages
   - Choose target language (RTL languages like Arabic are clearly marked)
   - Regional variants available (e.g., es-MX for Mexico, es-ES for Spain)

2. **Choose Translation Mode**
   - **Literal**: Direct word-for-word translation (fastest, most accurate for technical content)
   - **Localized**: Cultural adaptation with idiom replacement (recommended for most content)
   - **Transcreation**: Creative adaptation preserving emotional impact (best for marketing)

3. **Enter Content**
   - Paste text or script to translate
   - Supports both plain text and structured script lines

4. **Configure Options**
   - **Back-Translation QA**: Translates back to source language to verify accuracy (recommended)
   - **Timing Adjustment**: Adjusts timing for language expansion/contraction (enable for video content)

5. **Click Translate**
   - Translation typically completes in 5-15 seconds
   - For 10-minute video scripts: 30-45 seconds

#### Understanding Results

**Side-by-Side Comparison**
- Source and translated text displayed side-by-side
- Copy buttons for easy export

**Quality Metrics**
- **Overall Score**: Combined quality rating (target: >85%)
- **Fluency**: Grammar and natural flow (target: >80%)
- **Accuracy**: Meaning preservation (target: >80%)
- **Cultural Appropriateness**: Cultural sensitivity (target: >75%)
- **Terminology Consistency**: Glossary adherence (100% if using custom glossary)

**Back-Translation Verification**
- Shows translation converted back to source language
- Helps identify accuracy issues
- Score indicates similarity to original (target: >70%)

**Cultural Adaptations**
- Lists phrases adapted for cultural relevance
- Shows original → adapted phrase with reasoning
- Categories: Idioms, References, Measurements, etc.

**Timing Adjustments**
- Expansion factor (e.g., 1.3x for German)
- Original vs adjusted duration
- Compression suggestions if needed
- Warnings for significant timing changes

**Visual Recommendations**
- Flags visual elements needing localization
- Priority levels: High, Medium, Low
- Categories: Text-in-image, Cultural symbols, Colors, Gestures

### 2. Batch Translation

Translate content to multiple languages simultaneously for efficient multilingual video production.

#### How to Use

1. **Select Source Language**
   - Choose the language of your original content

2. **Select Target Languages**
   - Click dropdown to add languages
   - Selected languages shown as removable badges
   - Click badge with × to remove

3. **Enter Content**
   - Same as single translation

4. **Configure Options**
   - Same translation mode for all languages
   - Quality options apply to all

5. **Click Translate to N Languages**
   - Estimated time shown: ~15-45 seconds per language
   - Progress indicator shows queue status

#### Viewing Results

- Completed translations marked with green checkmark
- Click any completed translation to view detailed results
- Each translation includes full quality metrics and cultural adaptations
- Failed translations marked with error indicator

### 3. Glossary Management

Create and manage custom terminology glossaries to ensure consistent translation of brand names, product names, and technical terms.

#### Creating a Glossary

1. Click **Create Glossary**
2. Enter name (e.g., "Medical Terms", "Brand Names")
3. Optionally add description
4. Click Create

#### Adding Terms

1. Select a glossary and click **Add Entry**
2. Enter term in source language
3. Add translations for target languages
4. Click **+ Add Language** for additional translations
5. Optionally add context notes
6. Click Add Entry

#### Managing Entries

- View all entries in table format
- See translations across languages at a glance
- Context shown for reference

#### Exporting to CSV

1. Click **Export CSV** on any glossary
2. Downloads CSV with all terms and translations
3. Format: Term, Language1, Language2, ..., Context, Industry

#### Using Glossaries

- Glossaries automatically applied when translating
- Ensures 100% terminology consistency score
- Terms translated exactly as defined in glossary

### 4. Supported Languages (55+)

**Major Languages:**
- English (en, en-US, en-GB, en-AU)
- Spanish (es, es-ES, es-MX, es-AR)
- French (fr, fr-FR, fr-CA)
- German (de, de-DE, de-AT)
- Portuguese (pt-BR, pt-PT)
- Chinese (zh-CN, zh-TW, zh-HK)
- Arabic (ar, ar-SA, ar-EG) [RTL]
- Japanese (ja)
- Korean (ko)
- Italian (it)
- Russian (ru)
- Dutch (nl)
- Polish (pl)
- Turkish (tr)
- Thai (th)
- Vietnamese (vi)
- Indonesian (id)
- Malay (ms)
- Hindi (hi)
- Bengali (bn)
- Hebrew (he) [RTL]
- Persian (fa) [RTL]
- Urdu (ur) [RTL]

And 30+ more including:
- Czech, Slovak, Hungarian
- Swedish, Norwegian, Danish, Finnish
- Greek, Romanian, Bulgarian
- Ukrainian, Serbian, Croatian
- Tamil, Telugu, Kannada, Malayalam
- Tagalog, Swahili

**Regional Variants** automatically adjust:
- Formality level (tu vs usted in Spanish)
- Vocabulary preferences (lift vs elevator)
- Cultural references
- Date/time formats
- Measurement systems

## Best Practices

### 1. Translation Quality

- **Use Localized mode** for most content (balances accuracy and cultural relevance)
- **Enable back-translation QA** to catch accuracy issues
- **Review cultural adaptations** to ensure they align with your message
- **Pay attention to quality scores** - anything below 80% may need manual review

### 2. Cultural Sensitivity

- **Check visual recommendations** before finalizing videos
- **Review cultural adaptations** to ensure appropriateness
- **Consider regional variants** (Mexican Spanish vs Spain Spanish)
- **Test with native speakers** when possible

### 3. Timing for Video Content

- **Always enable timing adjustment** for video scripts
- **Review compression suggestions** if required
- **Test with narration speed** before final production
- **Consider shorter alternatives** for languages with high expansion factors

### 4. Glossary Usage

- **Create industry-specific glossaries** for technical content
- **Include brand names** to maintain consistency
- **Add context notes** for ambiguous terms
- **Export and backup** glossaries regularly
- **Share glossaries** across team via CSV export

### 5. Batch Translation Efficiency

- **Group similar content** types for batch translation
- **Use consistent translation mode** across related videos
- **Review high-priority languages first**
- **Plan for ~15-45 seconds per language**

## Troubleshooting

### Low Quality Scores

**Fluency < 80%:**
- Consider using Literal mode for technical content
- Check if source text has grammar issues
- Review for idiomatic expressions that don't translate well

**Accuracy < 80%:**
- Enable back-translation to identify issues
- Use glossary for important terms
- Consider breaking complex sentences into simpler ones

**Cultural Appropriateness < 75%:**
- Review cultural adaptations suggested
- Consider using Transcreation mode
- Check visual recommendations for problematic elements

### Timing Issues

**Requires Compression (>15% expansion):**
- Use compression suggestions provided
- Consider script rewording in source language
- Adjust narration speed in final production
- Split long scenes into multiple shorter scenes

**Critical Timing Warnings:**
- Review specific lines flagged
- Consider alternative phrasing
- May need to re-record audio at different speeds

### Visual Localization

**High Priority Recommendations:**
- Text-in-image: Must be recreated with translated text
- Cultural symbols: May need replacement
- Offensive gestures: Critical to address

**Medium/Low Priority:**
- Review and decide based on audience
- May not require immediate action
- Document for future iterations

## Performance Expectations

- **Single translation**: 5-15 seconds (short text) to 30-45 seconds (10-min script)
- **Batch translation**: 15-45 seconds per language
- **Quality scoring**: Included in translation time (no extra delay)
- **Back-translation**: Adds ~30% to translation time
- **Glossary lookup**: Negligible performance impact

## Keyboard Shortcuts

- **Tab**: Navigate through form fields
- **Enter**: Submit translation when focused on button
- **Space**: Expand/collapse results sections (when focused)
- **Escape**: Close dialogs

## Accessibility

- Full keyboard navigation support
- Screen reader compatible with ARIA labels
- High contrast mode supported
- RTL language layout automatically applied

## Tips for Best Results

1. **Start with English**: English has the most training data, producing best quality
2. **Use complete sentences**: Fragments may lose context in translation
3. **Avoid idioms in source**: Unless using Localized or Transcreation mode
4. **Test regional variants**: es-MX ≠ es-ES in tone and vocabulary
5. **Review adaptations**: AI suggestions are good but not perfect
6. **Build glossaries early**: Saves time on subsequent translations
7. **Batch similar content**: More efficient than one-by-one
8. **Check timing early**: Avoid surprises during production

## Advanced Features

### Custom Cultural Context

When translating with specific audience in mind:
1. Consider audience profile integration (if available)
2. Specify sensitivities and taboo topics
3. Choose appropriate formality level
4. Review cultural appropriateness scores carefully

### Integration with Video Production

1. Translate script before recording
2. Use timing adjustments to plan narration speed
3. Note visual recommendations for asset creation
4. Export glossary for translation services if needed
5. Re-use glossaries across video series

## Support

For issues or questions:
- Check quality scores and issues list in results
- Review cultural adaptations for unexpected changes
- Verify language codes are correct
- Ensure source text is clean (no formatting issues)
- Contact support with correlation ID from error messages

---

**Last Updated**: November 2024  
**Feature Version**: 1.0  
**Supported Languages**: 55+  
**Translation Modes**: Literal, Localized, Transcreation
