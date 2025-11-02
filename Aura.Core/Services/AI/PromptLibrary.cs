using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;

namespace Aura.Core.Services.AI;

/// <summary>
/// Library of curated few-shot examples for different video types
/// Provides examples that demonstrate ideal script structures and techniques
/// </summary>
public class PromptLibrary
{
    private readonly List<FewShotExample> _examples;

    public PromptLibrary()
    {
        _examples = InitializeExamples();
    }

    /// <summary>
    /// Get all available video types
    /// </summary>
    public IReadOnlyList<string> GetVideoTypes()
    {
        return _examples.Select(e => e.VideoType).Distinct().OrderBy(t => t).ToList();
    }

    /// <summary>
    /// Get examples for a specific video type
    /// </summary>
    public IReadOnlyList<FewShotExample> GetExamplesByType(string videoType)
    {
        return _examples
            .Where(e => e.VideoType.Equals(videoType, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Get a specific example by name
    /// </summary>
    public FewShotExample? GetExampleByName(string exampleName)
    {
        return _examples.FirstOrDefault(e => 
            e.ExampleName.Equals(exampleName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all examples
    /// </summary>
    public IReadOnlyList<FewShotExample> GetAllExamples()
    {
        return _examples;
    }

    /// <summary>
    /// Initialize curated few-shot examples
    /// </summary>
    private static List<FewShotExample> InitializeExamples()
    {
        return new List<FewShotExample>
        {
            // Educational Examples
            new FewShotExample(
                VideoType: "Educational",
                ExampleName: "Science Explainer",
                Description: "Clear, engaging explanation of complex scientific concepts",
                SampleBrief: "Topic: How does photosynthesis work? Audience: High school students. Goal: Teach the process step-by-step. Tone: Educational but engaging.",
                SampleOutput: @"# The Hidden Magic Inside Every Leaf

## Hook
What if I told you that plants are secretly running the most efficient solar panels on Earth? And they've been doing it for over 3 billion years.

## Introduction
Today, we're diving into photosynthesis - but forget everything you memorized for that test. We're going to see how plants literally turn sunlight into food, and why this process powers almost all life on Earth.

## The Solar Panel Setup
[VISUAL: Animation of leaf cross-section]
Inside every green leaf are millions of tiny structures called chloroplasts. Think of them as microscopic factories with one job: capture light energy.

## The Recipe for Life
Here's what plants need: sunlight, water from the roots, and carbon dioxide from the air. That's it. Three simple ingredients to create food and oxygen.

## The Light Reaction
[VISUAL: Chloroplast close-up with light hitting it]
When sunlight hits chlorophyll - that green pigment - it energizes electrons. These supercharged electrons split water molecules, releasing oxygen as a byproduct. Yes, every breath you take comes from this moment.

## The Dark Reaction (Calvin Cycle)
Now comes the magic trick. Using the energy from light, plants combine carbon dioxide with hydrogen to create glucose - sugar. This happens in a cycle that repeats six times to make one glucose molecule.

## Why It Matters
This process removes CO2 from our atmosphere, produces the oxygen we breathe, and creates the base of our entire food chain. Every meal you eat traces back to photosynthesis.

## Conclusion
Plants are silent superheroes, running 24/7 solar-powered factories that sustain all life on Earth. Next time you see a leaf, remember: you're looking at one of nature's most elegant solutions to energy conversion.",
                KeyTechniques: new[]
                {
                    "Hook with surprising fact",
                    "Break down complex process into simple steps",
                    "Use analogies (solar panels, factories)",
                    "Visual markers for key demonstrations",
                    "Connect to viewer's everyday life"
                }),

            new FewShotExample(
                VideoType: "Educational",
                ExampleName: "Historical Event",
                Description: "Narrative-driven history that brings events to life",
                SampleBrief: "Topic: The Fall of Constantinople in 1453. Audience: History enthusiasts. Goal: Explain historical significance. Tone: Dramatic yet informative.",
                SampleOutput: @"# The Day That Changed Two Worlds

## Hook
May 29, 1453. A thousand-year empire fell in a single day. The shockwaves would reshape Europe, spark the Renaissance, and redraw the map of the world.

## Setting the Stage
Constantinople. The greatest city of the medieval world. Home to half a million people. Protected by walls that had never been breached in a thousand years. Until Sultan Mehmed II arrived with 80,000 soldiers and something the world had never seen.

## The Impossible Weapon
[VISUAL: Ottoman cannons]
Mehmed brought cannons so massive they took three months to move into position. The largest could fire a 600-pound stone ball. These weren't just weapons - they were the end of an era.

## The Final Defense
Emperor Constantine XI had only 7,000 defenders. But they had those legendary walls - triple-layered fortifications that had stopped countless invaders. For 53 days, they held.

## The Secret Attack
[VISUAL: Map showing naval approach]
Here's where it gets incredible. The Byzantines had blocked the harbor with a massive chain. So Mehmed did something audacious: he hauled 70 ships over a mountain, bypassing the chain entirely.

## The Last Stand
On May 29th, after a final Ottoman assault, the walls finally broke. Constantine XI, last emperor of Rome, died fighting in the streets. An empire that traced its roots to Julius Caesar ended in the rubble.

## The Ripple Effect
This single event triggered a mass exodus of Greek scholars to Italy, carrying ancient texts and knowledge. This influx helped ignite the Renaissance. Meanwhile, the Ottoman Empire would dominate for 400 more years.

## Conclusion
The fall of Constantinople wasn't just the end of the Byzantine Empire. It was a hinge point in history - the moment when the medieval world became the modern world. Sometimes, everything changes in a day.",
                KeyTechniques: new[]
                {
                    "Dramatic opening with specific date",
                    "Build tension and stakes",
                    "Use vivid, concrete details",
                    "Show cause and effect",
                    "Connect to broader historical significance"
                }),

            // Entertainment Examples
            new FewShotExample(
                VideoType: "Entertainment",
                ExampleName: "Top 10 List",
                Description: "Engaging countdown format with personality and surprises",
                SampleBrief: "Topic: 10 Most Mind-Blowing Space Facts. Audience: General public. Goal: Entertain and amaze. Tone: Enthusiastic and accessible.",
                SampleOutput: @"# 10 Space Facts That Will Melt Your Brain

## Hook
Think space is just empty? Think again. What I'm about to tell you will completely change how you see the universe above us.

## Number 10: The Silent Scream
In space, no one can hear you scream. Not because sound doesn't exist - but because there's no air for sound waves to travel through. You could set off a nuclear bomb right next to you, and you'd see the flash but hear... nothing.

## Number 9: Cosmic Recycling
[VISUAL: Periodic table highlighting elements]
Every atom in your body older than 4.5 billion years was forged inside a dying star. You're literally made of stardust. The iron in your blood, the calcium in your bones - all came from exploding stars.

## Number 8: The Infinite Sunset
On the International Space Station, astronauts see 16 sunrises and 16 sunsets every single day. They orbit Earth every 90 minutes. Imagine watching the sun rise and set 16 times before breakfast.

## Number 7: The Diamond Planet
[VISUAL: Artist rendering of 55 Cancri e]
There's a planet called 55 Cancri e that's essentially a massive diamond. It's twice Earth's size and one-third pure diamond. Worth more than the entire global economy. Too bad it's 40 light-years away.

## Number 6: Time Travel is Real
GPS satellites have to account for relativity. They're moving so fast that time literally moves slower for them than for us on Earth. Without this correction, your GPS would be off by miles within minutes.

## Number 5: The Great Attractor
Something massive - something we can't even see - is pulling our entire galaxy toward it at 600 miles per second. We don't know what it is. We just know it's there, pulling us and millions of other galaxies.

## Number 4: Cold Welding
In space, if two pieces of metal touch, they can permanently fuse together. No heat needed. It's called cold welding, and it's caused by the vacuum of space. This is a nightmare for spacecraft engineers.

## Number 3: Boiling and Freezing
Water in space does something impossible: it boils and freezes at the same time. The lack of pressure makes it boil, while the cold makes it freeze. You end up with boiling ice.

## Number 2: The Void Between Atoms
Here's the real mind-bender: you're mostly empty space. If you removed all the empty space between atoms in every human on Earth, the entire human race would fit into a sugar cube. We're 99.9999999% empty space.

## Number 1: Observable Universe Has an Edge
The observable universe has a boundary - not because that's where space ends, but because that's as far as light has had time to reach us since the Big Bang. Beyond that edge, there's more universe. We just can't see it. Yet.

## Conclusion
Space isn't just out there - it's the most extreme, bizarre, beautiful laboratory in existence. And we're floating right in the middle of it, trying to make sense of it all. What a time to be alive.",
                KeyTechniques: new[]
                {
                    "Countdown format builds anticipation",
                    "Each fact has a 'wow' moment",
                    "Use concrete examples and numbers",
                    "Balance science with accessibility",
                    "Save biggest reveal for end"
                }),

            // Tutorial Examples
            new FewShotExample(
                VideoType: "Tutorial",
                ExampleName: "Technical How-To",
                Description: "Step-by-step instruction with clear demonstrations",
                SampleBrief: "Topic: How to Set Up a Home Recording Studio on a Budget. Audience: Aspiring musicians. Goal: Provide actionable steps. Tone: Helpful and practical.",
                SampleOutput: @"# Your First Home Studio: Under $500

## Hook
You don't need thousands of dollars to start recording professional-quality music at home. Let me show you exactly what you need and how to set it up.

## What You Actually Need
[VISUAL: Equipment laid out on desk]
Forget what you've heard. Here's the honest minimum: a decent audio interface, one good microphone, headphones, and free recording software. That's it. Everything else is optional.

## Step 1: The Audio Interface
This is your most important purchase. I recommend the Focusrite Scarlett Solo ($120). It connects your mic to your computer and converts analog sound to digital. Get this right, and you're 80% of the way there.

## Step 2: The Microphone
For vocals and acoustic instruments, get a large-diaphragm condenser mic. The Audio-Technica AT2020 ($100) punches way above its price. It's been used on actual albums.

## Step 3: Headphones
You need closed-back headphones for tracking. The Audio-Technica ATH-M50x ($150) are industry standard. They isolate well and give you an honest representation of your sound.

## Step 4: The Software (Free!)
[VISUAL: DAW interface]
Download Reaper. It's technically $60, but they give you an unlimited trial. Or use GarageBand if you're on Mac - it's free and powerful. You don't need Pro Tools yet.

## Step 5: Acoustic Treatment
Here's the secret: a $100 mic in a treated room beats a $1000 mic in a bare room. Hang moving blankets on your walls. Put a rug down. Close the closet door. Boom - instant improvement.

## The Setup Process
[VISUAL: Connection diagram]
Interface connects to computer via USB. Mic plugs into interface input 1. Headphones plug into interface headphone jack. Set your input gain until your meter peaks around -12dB. Record.

## Pro Tips for Better Recordings
Turn off your AC while recording - trust me on this. Record at night when it's quiet. Get the mic 6-12 inches from your mouth. Use a pop filter (or a sock stretched over a wire hanger - seriously).

## What to Skip (For Now)
Studio monitors? Wait until you know your room. MIDI controller? Your computer keyboard works fine. Expensive preamps? Your interface has preamps. Master these basics first.

## Conclusion
This $470 setup is all you need to record release-quality music. I've heard songs recorded with exactly this gear get radio play. The equipment doesn't make the music - you do. Now stop researching and start recording.",
                KeyTechniques: new[]
                {
                    "Clear, numbered steps",
                    "Specific product recommendations with prices",
                    "Anticipate and answer common questions",
                    "Visual markers for demonstrations",
                    "Actionable advice over theory"
                }),

            // Documentary Examples
            new FewShotExample(
                VideoType: "Documentary",
                ExampleName: "Investigation",
                Description: "In-depth exploration with journalistic approach",
                SampleBrief: "Topic: The Mystery of Dark Matter. Audience: Science-curious adults. Goal: Explore unknown phenomena. Tone: Investigative and thought-provoking.",
                SampleOutput: @"# The Invisible Universe We Can't See

## Hook
85% of the matter in our universe is missing. We can't see it, we can't touch it, we can't detect it directly. But we know it's there. And it's everywhere.

## The Discovery
[VISUAL: Galaxy rotation curves]
In the 1970s, astronomer Vera Rubin noticed something impossible. Galaxies were spinning too fast. Based on the visible matter, they should fly apart. But they don't. Something invisible was holding them together.

## The Evidence Mounts
It's not just one galaxy. Every galaxy we've studied shows the same pattern. The math only works if there's 5-6 times more matter than we can see. We named this phantom matter 'dark matter.'

## What It's Not
It's not planets. Not gas clouds. Not black holes. We've ruled out everything made of normal matter. Dark matter doesn't interact with light - it doesn't emit it, absorb it, or reflect it. It's fundamentally different from everything we've ever known.

## The Hunt Begins
[VISUAL: Underground detector facility]
Around the world, scientists have built ultra-sensitive detectors deep underground. They're waiting for dark matter particles to collide with normal matter. So far? Nothing. Decades of searching, and we still haven't directly detected a single dark matter particle.

## Alternative Theories
Some physicists wonder: what if there is no dark matter? What if gravity itself works differently at galactic scales? It's called Modified Newtonian Dynamics - MOND for short. But this creates more problems than it solves.

## Why It Matters
[VISUAL: Cosmic web simulation]
Dark matter shaped the universe. After the Big Bang, dark matter's gravity pulled normal matter together, forming the first galaxies. Without dark matter, stars, planets, and life itself might never have formed.

## The Current State
We're stuck in a strange place. We have overwhelming indirect evidence that dark matter exists. We can map where it is by how it bends light. We can simulate the entire universe's evolution - but only if dark matter is real. Yet we can't find it.

## What Comes Next
New detectors are coming online. More sensitive. Larger. Looking for different types of particles. Maybe dark matter will reveal itself tomorrow. Maybe it will take another 50 years. Maybe it's something so alien that our current physics can't even conceive of it.

## Conclusion
This is science at its most humbling. We've mapped the human genome, landed on the moon, split the atom. But 85% of the universe remains completely mysterious. Dark matter reminds us that for all our knowledge, we're still just beginning to understand reality.",
                KeyTechniques: new[]
                {
                    "Build mystery and suspense",
                    "Present evidence methodically",
                    "Acknowledge competing theories",
                    "Use expert perspectives implicitly",
                    "End with unresolved tension"
                }),

            // Promotional Examples
            new FewShotExample(
                VideoType: "Promotional",
                ExampleName: "Product Launch",
                Description: "Compelling product introduction that focuses on benefits",
                SampleBrief: "Topic: Introducing new noise-canceling earbuds. Audience: Tech consumers. Goal: Generate interest and desire. Tone: Exciting and aspirational.",
                SampleOutput: @"# The Sound of Nothing

## Hook
What if you could turn off the world with a single tap? No more crying babies on flights. No more subway screeching. Just you and your music.

## The Problem We All Face
[VISUAL: Montage of noisy environments]
Our world is loud. Constant. Exhausting. You can't focus on the coffee shop. Your commute is a cacophony. Even at home, your neighbor's dog won't stop barking. You just want peace.

## Introducing Eclipse Sound
We spent three years on a single mission: create the most effective noise cancellation ever put in wireless earbuds. The result? Eclipse Sound - and it's like nothing you've experienced.

## The Technology That Changes Everything
[VISUAL: Cutaway of earbud showing tech]
Six microphones per earbud sample environmental noise 50,000 times per second. Our AI processor generates inverse sound waves that cancel up to 99.5% of ambient noise. That's 10% more than the leading competitor.

## But Here's What Really Matters
Numbers are boring. Here's what this means: you can have a phone call in a construction zone and be heard perfectly. You can sleep on a redeye flight. You can work in a crowded space and actually concentrate.

## Sound Quality That Doesn't Compromise
[VISUAL: Frequency response graph]
Most noise-canceling earbuds sacrifice sound quality. Not Eclipse. Custom-tuned 12mm drivers deliver audiophile-grade sound. Bass that hits without overwhelming. Highs that sparkle. Mids that bring vocals to life.

## All-Day Comfort, All-Week Battery
Eight-hour battery life per charge. Another 24 hours in the case. That's a full work week without charging. And they weigh just 4.8 grams - you'll forget you're wearing them.

## Designed for Real Life
[VISUAL: Water resistance test]
IPX7 waterproof. Sweatproof. Run-in-the-rain proof. Touch controls that actually work. Instant pairing. Seamless switching between devices. They just... work.

## The Eclipse Promise
If you're not amazed within 30 days, send them back. No questions asked. We're that confident you'll never want to go back to regular earbuds.

## Conclusion
Life is loud. Eclipse Sound makes it quiet. Your sanctuary is just a tap away. Pre-order now and get 20% off launch price. Because everyone deserves to control their audio environment.",
                KeyTechniques: new[]
                {
                    "Lead with problem, not product",
                    "Use specific, verifiable claims",
                    "Focus on benefits over features",
                    "Include social proof implicitly",
                    "Clear call to action",
                    "Address objections proactively"
                })
        };
    }
}
