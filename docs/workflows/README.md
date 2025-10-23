# Workflows

Advanced guides for common workflows and use cases in Aura Video Studio.

## 📑 Available Workflows

- **[Portable Mode Guide](./PORTABLE_MODE_GUIDE.md)** - Using Aura in portable mode without installation
- **[Quick Demo Workflow](./QUICK_DEMO.md)** - Creating your first video in under 5 minutes
- **[Settings Schema](./SETTINGS_SCHEMA.md)** - Understanding and configuring application settings
- **[UX Best Practices](./UX_GUIDE.md)** - User experience guidelines and recommendations

## 🎯 Common Workflows

### 1. Quick Video Creation

**Goal**: Generate a video as fast as possible

**Steps**:
1. Launch Aura Video Studio
2. Click "Quick Demo" button
3. Wait for generation (2-3 minutes)
4. Review output in `Renders/` folder

**Use Case**: Testing, demonstrations, quick content

See [Quick Demo Guide](./QUICK_DEMO.md).

### 2. Educational Video Production

**Goal**: Create informative educational content

**Steps**:
1. Write detailed brief with learning objectives
2. Generate script using AI (GPT-4 recommended)
3. Review and edit for accuracy
4. Select relevant stock footage or generate images
5. Add professional narration (ElevenLabs recommended)
6. Generate captions for accessibility
7. Render at 1080p or higher

**Tips**:
- Use clear, simple language
- Include visual examples
- Add captions for accessibility
- Break complex topics into scenes
- Use consistent branding

### 3. Marketing Video Creation

**Goal**: Promotional content for products/services

**Steps**:
1. Define key message and call-to-action
2. Create compelling script with hook
3. Select high-quality visuals (custom or Stable Diffusion)
4. Use energetic voice and music
5. Add brand watermark and colors
6. Export in multiple formats for different platforms

**Tips**:
- Hook viewers in first 3 seconds
- Keep it short (30-60 seconds)
- Clear call-to-action
- Brand-consistent visuals
- Optimize for mobile viewing

### 4. Social Media Content

**Goal**: Engaging content for social platforms

**Steps**:
1. Choose platform (Instagram, TikTok, YouTube Shorts)
2. Select appropriate aspect ratio (9:16 for vertical)
3. Write punchy, attention-grabbing script
4. Use trending visual styles
5. Add captions (essential for silent autoplay)
6. Export at platform-optimized settings

**Platform Specs**:
- **Instagram Reels**: 9:16, 1080x1920, 15-90s
- **TikTok**: 9:16, 1080x1920, 15-180s
- **YouTube Shorts**: 9:16, 1080x1920, up to 60s
- **YouTube**: 16:9, 1920x1080, any length
- **Twitter**: 16:9, 1280x720, up to 2:20

### 5. Batch Processing

**Goal**: Generate multiple videos efficiently

**Steps**:
1. Prepare briefs in JSON format:
   ```json
   {
     "videos": [
       {
         "title": "Video 1",
         "description": "Description 1"
       },
       {
         "title": "Video 2",
         "description": "Description 2"
       }
     ]
   }
   ```

2. Use CLI for batch processing:
   ```bash
   aura batch --input briefs.json --output ./videos/
   ```

3. Monitor progress and logs

4. Review all outputs

**Use Case**: Series production, A/B testing, scalable content

### 6. Template-Based Production

**Goal**: Reuse proven video structures

**Steps**:
1. Create or select template
2. Define variable fields (title, key points, etc.)
3. Fill in template data
4. Generate multiple variations
5. Compare and select best version

**Benefits**:
- Consistent quality
- Faster production
- Easier delegation
- Brand consistency

### 7. Custom Asset Workflow

**Goal**: Use your own images and videos

**Steps**:
1. Organize assets in folders:
   ```
   Assets/
   ├── Images/
   │   ├── intro.jpg
   │   └── product.png
   └── Videos/
       └── demo.mp4
   ```

2. Import to asset library

3. Tag and categorize assets

4. Reference in timeline

5. Render with custom assets

**Tips**:
- Use consistent naming
- High resolution (1080p+)
- Proper aspect ratio
- Organized by project

### 8. Multi-Language Production

**Goal**: Create videos in multiple languages

**Steps**:
1. Create master script in primary language
2. Translate script to target languages
3. Generate narration in each language
4. Use same visual assets
5. Create language-specific captions
6. Export separate videos per language

**Supported Languages**:
- English, Spanish, French, German, Italian
- Portuguese, Dutch, Polish, Russian
- Chinese, Japanese, Korean
- Arabic, Hindi, and more

See TTS provider documentation for full list.

### 9. Accessibility-Focused Workflow

**Goal**: Create accessible content for all audiences

**Steps**:
1. Write clear, simple script
2. Generate accurate captions
3. Ensure high color contrast
4. Use descriptive audio (describe visuals)
5. Provide transcript
6. Test with screen readers

**Accessibility Features**:
- ✅ Closed captions (SRT, VTT)
- ✅ Audio descriptions
- ✅ High contrast text
- ✅ Keyboard navigation
- ✅ WCAG 2.1 AA compliant

### 10. Professional Review Workflow

**Goal**: Produce publication-ready content

**Steps**:
1. Generate draft video
2. Internal review:
   - Script accuracy
   - Visual quality
   - Audio clarity
   - Brand compliance
3. Make revisions in timeline editor
4. Second review
5. Final approval
6. Export at highest quality
7. Archive source files

**Review Checklist**:
- [ ] Script is accurate and engaging
- [ ] Visuals match narration
- [ ] Audio is clear and normalized
- [ ] Captions are synced
- [ ] Branding is consistent
- [ ] No errors or typos
- [ ] Meets quality standards
- [ ] Proper export settings

## 🔄 Workflow Automation

### Using the CLI

Automate repetitive tasks:

```bash
# Generate script only
aura script --brief "My video brief" --output script.txt

# Generate and render in one command
aura generate --brief "My video" --auto-render

# Watch folder for new briefs
aura watch --input ./briefs/ --output ./videos/
```

### Using the API

Integrate with other tools:

```python
import requests

# Create video via API
response = requests.post('http://localhost:5005/api/v1/generate', json={
    'brief': {
        'title': 'My Video',
        'description': 'Video description'
    },
    'settings': {
        'quality': 'high',
        'resolution': '1080p'
    }
})

video_path = response.json()['path']
```

### Scheduled Generation

Use system scheduler (cron/Task Scheduler):

```bash
# Linux cron
0 2 * * * cd /path/to/aura && aura batch --input daily-briefs.json

# Windows Task Scheduler
schtasks /create /tn "Aura Daily" /tr "aura batch --input daily-briefs.json" /sc daily /st 02:00
```

## 📊 Workflow Optimization

### Performance Tips

1. **Pre-download assets**: Cache stock footage locally
2. **Use templates**: Reduce generation time
3. **Batch similar videos**: Reuse provider calls
4. **GPU acceleration**: Enable for faster rendering
5. **SSD storage**: Use for projects and cache

### Quality Tips

1. **Review before rendering**: Check timeline carefully
2. **Test with samples**: Render short clips first
3. **Use high-quality assets**: 1080p or higher
4. **Professional audio**: Invest in good TTS
5. **Proofread scripts**: Catch errors early

### Cost Optimization

1. **Mix providers**: Free for testing, pro for production
2. **Cache API responses**: Reuse generated content
3. **Optimize provider calls**: Batch requests
4. **Monitor usage**: Track API costs
5. **Use local when possible**: Windows TTS, local SD

## 🎓 Learning Resources

### Tutorials

- [Quick Demo](./QUICK_DEMO.md) - First video walkthrough
- [Portable Mode](./PORTABLE_MODE_GUIDE.md) - No-install usage
- [Settings](./SETTINGS_SCHEMA.md) - Configuration guide
- [UX Guide](./UX_GUIDE.md) - Interface tips

### Best Practices

- [Quality Guidelines](../best-practices/README.md)
- [Performance Optimization](../best-practices/README.md)
- [Provider Selection](../best-practices/README.md)

### Advanced Topics

- [API Integration](../api/README.md)
- [CLI Automation](../features/CLI.md)
- [Custom Providers](../api/providers.md)

## 💡 Workflow Examples

### Example 1: Daily News Summary

```bash
# Fetch news headlines (external script)
python fetch_news.py > brief.txt

# Generate video
aura generate --brief-file brief.txt --output daily-news.mp4

# Upload to platform (external script)
python upload_video.py daily-news.mp4
```

### Example 2: Product Demo Series

```json
{
  "template": "product-demo",
  "videos": [
    {
      "product": "Widget Pro",
      "features": ["Feature 1", "Feature 2", "Feature 3"],
      "cta": "Visit example.com"
    },
    {
      "product": "Widget Plus",
      "features": ["Feature A", "Feature B", "Feature C"],
      "cta": "Visit example.com"
    }
  ]
}
```

### Example 3: Course Content

```yaml
course: Introduction to Python
module: 1
lessons:
  - title: "Variables and Data Types"
    duration: 180
    key_points:
      - "What are variables"
      - "Common data types"
      - "Type conversion"
  
  - title: "Control Flow"
    duration: 240
    key_points:
      - "If statements"
      - "Loops"
      - "Functions"
```

## 🤝 Community Workflows

Share your workflows with the community!

1. Document your workflow
2. Include example files
3. Submit to [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)
4. Tag as "workflow"

---

**Need help with a specific workflow?** Ask in [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)!
