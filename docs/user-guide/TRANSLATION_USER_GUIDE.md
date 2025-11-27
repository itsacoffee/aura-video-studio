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

## Advanced Features

### 5. Subtitle Generation and Export

Aura Video Studio automatically generates subtitles from translated scripts with precise timing alignment, supporting both burn-in (embedded) and external file formats.

#### Subtitle Formats

**SRT (SubRip Text)**
- Standard format supported by most video players
- Simple text format with timecodes
- File extension: `.srt`
- Use for: Maximum compatibility

**VTT (WebVTT)**
- Web-standard subtitle format
- Supports styling and positioning
- File extension: `.vtt`
- Use for: Web videos, HTML5 players

#### Timing Alignment

**Automatic Duration Matching:**
- Subtitles automatically adjusted to match target TTS duration
- ±2% tolerance ensures accurate synchronization
- Timing validation prevents overlaps and gaps
- Expansion/contraction handled per language characteristics

**Timing Validation:**
- Pre-export validation checks for:
  - Non-overlapping timecodes
  - Positive durations
  - Proper sequencing
  - Duration tolerance compliance

#### Subtitle Export Options

**External Files (Recommended for flexibility):**
1. Navigate to translation results
2. Click "Export Subtitles"
3. Select format (SRT or VTT)
4. Choose output location
5. File saved with UTF-8 encoding

**Burn-In (Embedded in video):**
1. Enable "Burn Subtitles" during rendering
2. Configure styling options:
   - Font family and size
   - Text color and outline
   - Position (bottom center recommended)
   - Background style (transparent or opaque box)
3. Subtitles rendered directly into video frames
4. Cannot be disabled or changed after rendering

#### RTL (Right-to-Left) Language Support

Aura Studio provides comprehensive RTL support for Arabic, Hebrew, Persian, and Urdu subtitles.

**Automatic RTL Detection:**
- System automatically detects RTL languages
- Font fallback to Unicode-compatible fonts (Arial Unicode MS, Tahoma)
- Text alignment adjusted for RTL reading direction
- Proper bidirectional text handling

**RTL Subtitle Features:**
- Arabic (ar, ar-SA, ar-EG)
- Hebrew (he)
- Persian/Farsi (fa)
- Urdu (ur)

**RTL Styling Options:**
- Font fallback: Arial Unicode MS (default for RTL)
- Text direction: Automatic right-to-left
- Alignment: Properly positioned for RTL reading
- Mixed content: Handles numbers and Latin text within RTL text

**Best Practices for RTL:**
1. Always preview RTL subtitles before final rendering
2. Use recommended font fallbacks for proper glyph rendering
3. Test on target playback devices
4. Verify number and punctuation placement
5. Check for proper handling of mixed LTR/RTL content

#### Voice Recommendations per Language

Get AI-recommended voices tailored to each target language with appropriate accent, style, and quality tier.

**How It Works:**
1. Select target language
2. Choose TTS provider (ElevenLabs, PlayHT, Windows SAPI, Piper)
3. System recommends voices based on:
   - Native language support
   - Voice quality tier
   - Style appropriateness (professional, conversational, warm)
   - Gender options

**Provider Recommendations:**

**ElevenLabs (Premium):**
- Spanish: Diego (Male/Professional), Sofia (Female/Warm)
- French: Antoine (Male/Professional), Charlotte (Female/Elegant)
- German: Hans (Male/Authoritative), Greta (Female/Professional)
- Japanese: Akira (Male/Professional), Sakura (Female/Gentle)
- Chinese: Li Wei (Male/Professional), Mei Lin (Female/Warm)
- Arabic: Ahmed (Male/Professional), Fatima (Female/Warm)

**PlayHT (Premium):**
- Adaptive voice selection per language
- Voice cloning support
- High-quality neural synthesis

**Windows SAPI (Free):**
- Native Windows voices per system language
- Standard quality
- Offline capability

**Piper (Free, Offline):**
- Neural TTS for multiple languages
- Privacy-focused (fully offline)
- Good quality for free option

**RTL Language Voice Tips:**
- Arabic: Use native Arabic voices for authentic pronunciation
- Hebrew: Select Hebrew-native voices for proper emphasis
- Persian: Ensure voice supports Persian-specific phonetics
- Test with sample text before full production

### 6. Translation with SSML Integration

Advanced workflow combining translation, SSML markup, and TTS generation for complete localized video production.

**Integrated Pipeline:**
1. **Translation**: Script translated to target language with cultural adaptation
2. **SSML Planning**: Prosody and timing adjusted for target voice provider
3. **TTS Synthesis**: Audio generated with optimized SSML markup
4. **Subtitle Generation**: Perfectly aligned subtitles from SSML timing markers

**Benefits:**
- Single-click workflow from script to audio + subtitles
- Timing automatically synchronized across translation and audio
- SSML optimizations ensure natural-sounding narration
- Subtitles guaranteed to match audio timing within ±2% tolerance

**Usage:**
1. Select source and target languages
2. Choose TTS provider and voice
3. Enable "Generate with SSML and Subtitles"
4. System handles complete pipeline automatically
5. Receive: translated audio + synchronized subtitles + translation report

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

### Structured Output Instead of Translation (Ollama/Local LLMs)

**Problem:** Translation returns JSON-like output with metadata fields (e.g., `{"title": "...", "description": "..."}`) instead of clean translated text.

**Cause:** Local LLM (Ollama) is interpreting the translation prompt incorrectly, generating structured documentation or tutorial content rather than pure translation output.

**Solutions:**

1. **Update to Latest Version**
   - The latest version includes improved prompt engineering that provides stronger output constraints
   - Prompts now include explicit instructions to avoid JSON/structured output

2. **Try a Different Ollama Model**
   - Some models (e.g., llama3.1, mistral) follow translation instructions more reliably
   - Models with larger context windows tend to perform better
   - Check available models with: `ollama list`
   - Install a recommended model with: `ollama pull llama3.1:8b`

3. **Adjust Model Temperature**
   - Lower temperature values (0.3-0.5) produce more consistent output
   - Higher temperatures may cause the model to "improvise" with structured formats
   - Configure in your LLM settings or provider configuration

4. **Verify Model Context Length**
   - Ensure the model has sufficient context length for your translation
   - Long source texts may cause truncation issues
   - Consider breaking long texts into smaller segments

5. **Automatic Recovery**
   - The system now automatically attempts to extract translation from JSON responses
   - If a `translation`, `translatedText`, `text`, or `content` field is detected, it will be extracted
   - Check logs for warnings about "structured JSON response" if issues persist

**Technical Details:**
- The translation service includes robust response parsing that handles malformed output
- JSON artifacts are automatically stripped when detected
- Provider-specific prompt reinforcement is applied for local models

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

## Advanced Features: Translation with Video Production

### Translation Mode for Video Generation

Aura Video Studio now supports integrated translation workflows that combine translation, text-to-speech, and subtitle generation for localized video production.

#### Workflow Overview

1. **Translate Script** → 2. **Generate SSML with Timing** → 3. **Synthesize Speech** → 4. **Generate Subtitles** → 5. **Render Video**

#### How to Use Translation Mode

**Step 1: Prepare Your Source Script**
- Create or import your video script in the source language
- Ensure timing information is accurate for each scene
- Review script quality before translation

**Step 2: Select Target Language and Voice**
- Choose target language from 55+ supported languages
- System automatically detects RTL languages (Arabic, Hebrew, Persian, Urdu)
- Get voice recommendations for the target language
  - Premium voices: ElevenLabs, PlayHT (requires API keys)
  - Free voices: Windows SAPI, Piper (offline capable)

**Step 3: Configure Translation Options**
- Select translation mode (Literal, Localized, or Transcreation)
- Enable timing adjustment for language expansion
- Configure quality scoring for validation

**Step 4: Generate Translated Video**
- System automatically:
  - Translates script with cultural adaptation
  - Plans SSML with timing alignment to match original durations
  - Generates subtitles in SRT or VTT format
  - Selects appropriate voice for target language
  - Renders video with synchronized audio and subtitles

#### Voice Recommendations

The system provides intelligent voice recommendations based on:
- **Target language**: Native voices for each language
- **Provider availability**: Premium vs. free options
- **Gender and style preferences**: Male/female, professional/conversational/warm
- **RTL support**: Automatic font and layout adjustments

**Example Recommendations:**
- **Spanish (es)**: Diego (Male, Professional), Sofia (Female, Warm), Matias (Male, Conversational)
- **French (fr)**: Antoine (Male, Professional), Charlotte (Female, Elegant), Thomas (Male, Friendly)
- **Arabic (ar)**: Ahmed (Male, Professional), Fatima (Female, Warm), Omar (Male, Authoritative) - RTL layout enabled
- **Japanese (ja)**: Akira (Male, Professional), Sakura (Female, Gentle), Takeshi (Male, Dynamic)

### Subtitle Generation and Preview

#### Automatic Subtitle Generation

When translating for video, subtitles are automatically generated with:
- **Accurate timing**: Synchronized with SSML timing markers
- **Format options**: SRT (SubRip) or VTT (WebVTT)
- **Quality validation**: Checks for overlapping timecodes and timing issues

#### Subtitle Preview

Preview subtitles before rendering:
- **Real-time display**: See subtitles with proper timing
- **RTL layout**: Automatic right-to-left text alignment for RTL languages
- **Font preview**: See how subtitles will appear with selected fonts
- **Download option**: Export subtitles as separate files

#### Font Configuration for Subtitles

Configure subtitle appearance with RTL support:

**Font Settings:**
- **Font Family**: Choose from system fonts with automatic RTL fallbacks
  - LTR languages: Arial, Helvetica, Verdana, Segoe UI, Roboto
  - RTL languages: Noto Sans Arabic, Tahoma, Microsoft Sans Serif
- **Font Size**: Adjustable from 12-48pt (default: 24pt)
- **Text Color**: Hex color code (default: FFFFFF white)
- **Outline Color**: Hex color code for text outline (default: 000000 black)
- **Outline Width**: Thickness of outline (0-5px, default: 2px)
- **Text Alignment**: Left, Center, Right (auto-selected for RTL)

**RTL Font Fallbacks:**
- Arabic: Noto Sans Arabic, Tahoma
- Hebrew: Noto Sans Hebrew, Arial
- Persian/Urdu: Noto Nastaliq Urdu, Tahoma

### RTL (Right-to-Left) Language Support

#### Automatic RTL Detection

The system automatically detects and configures RTL layout for:
- **Arabic** (ar, ar-SA, ar-EG, etc.)
- **Hebrew** (he)
- **Persian** (fa)
- **Urdu** (ur)

#### RTL Features

**User Interface:**
- Direction: Automatic text-align right
- Layout: Flex-direction reversed
- Margins/Padding: Logical properties (inline-start/inline-end)

**Subtitles:**
- Text direction: RTL with proper Unicode bidi support
- Font stacks: RTL-optimized font fallbacks
- Alignment: Right-aligned by default

**Visual Indicators:**
- RTL badge displayed in language selection
- RTL indicator in subtitle preview
- Font recommendations include RTL-optimized options

#### Best Practices for RTL Content

1. **Font Selection**: Always use recommended RTL fonts
2. **Text Length**: RTL languages may have different expansion factors
3. **Visual Elements**: Review visual recommendations for text-in-image
4. **Testing**: Preview subtitles before final render
5. **Quality Check**: Verify cultural appropriateness scores

### Timing Alignment with SSML

#### SSML Planning Integration

The translation system integrates with SSML (Speech Synthesis Markup Language) planning to ensure:
- **Duration matching**: Translated speech matches original timing
- **Natural pauses**: Appropriate breaks between sentences
- **Prosody adjustments**: Rate, pitch, and volume tuning
- **Emphasis markers**: Highlight important words
- **Timing markers**: Synchronization points for subtitles

#### Timing Tolerance

Configure timing tolerance for translation:
- **Default**: ±2% deviation from target duration
- **Tight**: ±1% for precise synchronization
- **Relaxed**: ±5% for more natural speech

#### Handling Timing Expansion

Some languages naturally expand or contract compared to English:
- **Expansion**: German (+30%), French (+20%), Spanish (+20%)
- **Contraction**: Chinese (-30%), Japanese (-20%)

The system automatically:
1. Adjusts speech rate within natural limits
2. Suggests text compression if needed
3. Provides warnings for critical timing issues
4. Maintains quality while meeting duration targets

## API Integration

For developers integrating translation features:

### Translation with SSML Endpoint

```http
POST /api/localization/translate-and-plan-ssml
Content-Type: application/json

{
  "sourceLanguage": "en",
  "targetLanguage": "es",
  "scriptLines": [
    {
      "sceneIndex": 0,
      "text": "Welcome to our tutorial",
      "startSeconds": 0.0,
      "durationSeconds": 3.0
    }
  ],
  "targetProvider": "ElevenLabs",
  "voiceSpec": {
    "voiceName": "Diego",
    "rate": 1.0,
    "pitch": 0.0,
    "volume": 1.0
  },
  "subtitleFormat": "SRT"
}
```

**Response includes:**
- Translated script with adjusted timing
- SSML markup with prosody adjustments
- Generated subtitles in requested format
- Quality metrics and warnings

### Voice Recommendation Endpoint

```http
POST /api/localization/voice-recommendation
Content-Type: application/json

{
  "targetLanguage": "ar",
  "provider": "ElevenLabs",
  "preferredGender": "Female",
  "preferredStyle": "Professional"
}
```

**Response includes:**
- List of recommended voices
- RTL indicator for the language
- Voice characteristics (gender, style, quality tier)

## Support

For issues or questions:
- Check quality scores and issues list in results
- Review cultural adaptations for unexpected changes
- Verify language codes are correct
- Ensure source text is clean (no formatting issues)
- Preview subtitles for RTL layout correctness
- Test voice recommendations before full production
- Contact support with correlation ID from error messages

---

**Last Updated**: November 2024  
**Feature Version**: 1.1  
**Supported Languages**: 55+  
**Translation Modes**: Literal, Localized, Transcreation  
**New Features**: Translation with SSML, Subtitle Generation, RTL Support, Voice Recommendations
