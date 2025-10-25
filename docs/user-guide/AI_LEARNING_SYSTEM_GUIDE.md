# AI Learning System - User Guide

## Overview

The AI Learning System (PR 25) analyzes user decisions to identify patterns, infer preferences, and improve AI suggestions over time. The system learns from each decision you make and adapts to your unique preferences.

## Key Features

### 1. Decision Analysis
The system tracks every decision you make on AI suggestions:
- **Acceptance**: When you accept a suggestion as-is
- **Rejection**: When you decline a suggestion
- **Modification**: When you adjust a suggestion before using it

### 2. Pattern Recognition
After collecting decisions, the system identifies patterns in your behavior:
- **Acceptance Patterns**: Types of suggestions you consistently accept
- **Rejection Patterns**: Types of suggestions you consistently reject
- **Modification Patterns**: How you typically adjust suggestions

### 3. Preference Inference
Based on patterns, the system infers your preferences:
- **Tone Preferences**: Formality, energy levels
- **Visual Preferences**: Aesthetic, color palette, composition
- **Audio Preferences**: Music genres, sound effects usage
- **Editing Preferences**: Pacing, cut frequency, transition styles

### 4. Predictive Ranking
The system ranks future suggestions based on predicted acceptance:
- Higher-ranked suggestions are more likely to match your preferences
- Lower-ranked suggestions can be filtered out automatically
- Each prediction comes with confidence scores and explanations

## API Endpoints

### Get Patterns
```
GET /api/learning/patterns/{profileId}
```
Returns identified patterns for a profile.

### Get Insights
```
GET /api/learning/insights/{profileId}
```
Returns actionable learning insights.

### Trigger Analysis
```
POST /api/learning/analyze
Body: { "profileId": "..." }
```
Triggers pattern analysis and preference inference.

### Get Prediction Statistics
```
GET /api/learning/predictions/{profileId}
```
Returns acceptance/rejection rates by category.

### Rank Suggestions
```
POST /api/learning/rank-suggestions
Body: {
  "profileId": "...",
  "suggestionType": "visual",
  "suggestions": [...]
}
```
Ranks suggestions by predicted acceptance probability.

### Get Confidence Score
```
GET /api/learning/confidence/{profileId}/{suggestionType}
```
Returns AI confidence level for a specific category.

### Reset Learning
```
DELETE /api/learning/reset/{profileId}
```
Clears all learning data for a profile (starts fresh).

### Get Maturity Level
```
GET /api/learning/maturity/{profileId}
```
Returns learning maturity: nascent, developing, mature, or expert.

### Confirm Preference
```
POST /api/learning/confirm-preference
Body: {
  "profileId": "...",
  "preferenceId": "...",
  "isCorrect": true/false,
  "correctedValue": "..." (optional)
}
```
Confirms or corrects an inferred preference.

### Get Inferred Preferences
```
GET /api/learning/preferences/{profileId}
```
Returns all inferred preferences with confidence scores.

### Get Analytics
```
GET /api/learning/analytics/{profileId}
```
Returns comprehensive learning analytics and statistics.

## Frontend Components

### LearningDashboard
Main dashboard showing overview of AI learning progress:
- Learning maturity level
- Identified patterns
- Insights and recommendations
- Inferred preferences

### PatternList
Displays identified patterns with:
- Pattern type (acceptance, rejection, modification)
- Strength score
- Occurrence count

### InsightCard
Shows individual learning insights with:
- Description of the insight
- Confidence level
- Actionable recommendations

### ConfidenceIndicator
Visual indicator showing AI confidence level:
- Low (0-40%): Gray
- Medium (40-70%): Yellow
- High (70-100%): Green

### LearningProgress
Progress indicator showing:
- Total decisions made
- Learning maturity level
- Category-specific progress
- Strong and weak areas

### PreferenceConfirmation
Prompts user to confirm inferred preferences:
- Shows inferred preference
- Confidence level
- Option to confirm or correct

### SuggestionExplainer
Explains why AI made a suggestion:
- Prediction probabilities
- Reasoning factors
- Similar past decisions

## Learning Maturity Levels

1. **Nascent** (<20 decisions)
   - AI is just beginning to learn
   - Low confidence in predictions
   - Needs more data

2. **Developing** (20-50 decisions)
   - AI is learning patterns
   - Moderate confidence
   - Some predictions available

3. **Mature** (50-100 decisions)
   - AI has good understanding
   - High confidence in predictions
   - Reliable recommendations

4. **Expert** (100+ decisions)
   - AI has deep understanding
   - Very high confidence
   - Highly personalized suggestions

## Best Practices

### For Users

1. **Be Consistent**: Make decisions consistently to help the AI learn faster
2. **Confirm Preferences**: When the AI asks to confirm a preference, take a moment to verify
3. **Analyze Regularly**: Trigger analysis after making 10-20 new decisions
4. **Review Insights**: Check the insights tab regularly for actionable recommendations
5. **Use Multiple Profiles**: Create different profiles for different content types

### For Developers

1. **Record Decisions**: Always record user decisions with context
2. **Include Context**: Provide relevant context (tone, aesthetic, etc.) when recording decisions
3. **Handle Errors**: Learning endpoints may return empty results for new profiles
4. **Check Maturity**: Verify maturity level before relying on predictions
5. **Respect Privacy**: Learning data is profile-specific and isolated

## Data Storage

Learning data is stored in:
```
%LOCALAPPDATA%\Aura\Learning\
├── Patterns\       # Identified patterns per profile
├── Insights\       # Generated insights per profile
└── InferredPreferences\  # Inferred preferences per profile
```

## Success Metrics

The learning system aims for:
- **Pattern identification** after 20-30 decisions per category
- **80%+ accuracy** for high-confidence predictions
- **30%+ improvement** in suggestion acceptance rates
- **Isolated learning** per profile (no cross-contamination)

## Troubleshooting

### No Patterns Identified
- Ensure at least 5 decisions have been recorded
- Check that decisions have appropriate context
- Trigger manual analysis via API

### Low Confidence Scores
- Make more decisions in the category
- Ensure decisions are consistent
- Review and confirm inferred preferences

### Reset Learning
If the AI seems to have learned incorrect patterns:
1. Go to Learning Dashboard
2. Click "Reset Learning"
3. Confirm reset
4. Start making decisions again

## Example Usage

### TypeScript/React
```typescript
import { learningService } from '@/services/learning/learningService';
import { LearningDashboard } from '@/components/Learning';

// Get learning data
const patterns = await learningService.getPatterns(profileId);
const insights = await learningService.getInsights(profileId);
const maturity = await learningService.getMaturityLevel(profileId);

// Rank suggestions
const ranked = await learningService.rankSuggestions({
  profileId,
  suggestionType: 'visual',
  suggestions: myProposals
});

// Display dashboard
<LearningDashboard profileId={currentProfileId} />
```

## Dependencies

The learning system depends on:
- **PR 24**: User Profile System (for decision recording)
- Profile-specific decision history
- Persistent storage for learned patterns

## Future Enhancements

Potential future improvements:
- Cross-profile pattern detection
- Temporal pattern analysis (time-based trends)
- A/B testing of predictions
- Export/import learning data
- Learning visualization timeline
