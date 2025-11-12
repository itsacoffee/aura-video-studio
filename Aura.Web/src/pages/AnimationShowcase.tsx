import {
  PlayRegular,
  ChevronRightRegular,
  CheckmarkRegular,
  DismissRegular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import {
  AnimatedList,
  AnimatedListItem,
  FadeIn,
  SlideIn,
  ScaleIn,
} from '../components/animations';
import {
  SuccessAnimation,
  ErrorAnimation,
  ProgressIndicator,
} from '../components/feedback';
import {
  Skeleton,
  SkeletonText,
  SkeletonCard,
  SkeletonList,
  LoadingSpinner,
  LoadingDots,
  LoadingBar,
} from '../components/Loading';
import {
  AnimatedButton,
  AnimatedCard,
  AnimatedCardHeader,
  AnimatedCardBody,
  AnimatedCardFooter,
  AnimatedInput,
  AnimatedModal,
  AnimatedModalFooter,
  AnimatedTooltip,
} from '../components/ui';

/**
 * Animation Showcase Page
 * Demonstrates all animation components and interactions
 */
export function AnimationShowcase() {
  const [showSuccess, setShowSuccess] = useState(false);
  const [showError, setShowError] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [progress, setProgress] = useState(45);
  const [isLoading, setIsLoading] = useState(false);

  const handleLoadingDemo = () => {
    setIsLoading(true);
    setTimeout(() => {
      setIsLoading(false);
      setShowSuccess(true);
      setTimeout(() => setShowSuccess(false), 2000);
    }, 2000);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 p-8">
      <div className="max-w-7xl mx-auto space-y-12">
        {/* Header */}
        <FadeIn>
          <div className="text-center space-y-4">
            <h1 className="text-5xl font-bold bg-gradient-to-r from-primary-600 to-secondary-600 bg-clip-text text-transparent">
              Animation System Showcase
            </h1>
            <p className="text-xl text-gray-600 dark:text-gray-400">
              Professional UI animations with accessibility support
            </p>
          </div>
        </FadeIn>

        {/* Buttons Section */}
        <SlideIn direction="fromLeft">
          <AnimatedCard>
            <AnimatedCardHeader
              title="Interactive Buttons"
              subtitle="Smooth hover and press animations"
            />
            <AnimatedCardBody>
              <div className="flex flex-wrap gap-4">
                <AnimatedButton variant="primary" leftIcon={<PlayRegular />}>
                  Primary Button
                </AnimatedButton>
                <AnimatedButton variant="secondary">Secondary Button</AnimatedButton>
                <AnimatedButton variant="outline" rightIcon={<ChevronRightRegular />}>
                  Outline Button
                </AnimatedButton>
                <AnimatedButton variant="ghost">Ghost Button</AnimatedButton>
                <AnimatedButton variant="danger" leftIcon={<DismissRegular />}>
                  Danger Button
                </AnimatedButton>
                <AnimatedButton
                  variant="primary"
                  isLoading={isLoading}
                  onClick={handleLoadingDemo}
                >
                  {isLoading ? 'Processing...' : 'Click to Load'}
                </AnimatedButton>
              </div>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Input Section */}
        <SlideIn direction="fromRight" delay={0.1}>
          <AnimatedCard>
            <AnimatedCardHeader
              title="Animated Inputs"
              subtitle="Smooth focus effects and validation states"
            />
            <AnimatedCardBody>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <AnimatedInput label="Email Address" type="email" placeholder="you@example.com" />
                <AnimatedInput
                  label="Password"
                  type="password"
                  placeholder="••••••••"
                  hint="Must be at least 8 characters"
                />
                <AnimatedInput
                  label="With Error"
                  type="text"
                  error="This field is required"
                  placeholder="Enter value"
                />
                <AnimatedInput label="Disabled" type="text" disabled placeholder="Disabled input" />
              </div>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Cards Section */}
        <SlideIn direction="fromLeft" delay={0.2}>
          <div className="space-y-4">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
              Interactive Cards
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <AnimatedCard variant="elevated" interactive>
                <AnimatedCardHeader title="Elevated Card" subtitle="Hover for effect" />
                <AnimatedCardBody>
                  <p>This card has a subtle elevation and smooth hover animation.</p>
                </AnimatedCardBody>
                <AnimatedCardFooter>
                  <AnimatedButton size="sm" variant="outline">
                    Learn More
                  </AnimatedButton>
                </AnimatedCardFooter>
              </AnimatedCard>

              <AnimatedCard variant="outlined" interactive>
                <AnimatedCardHeader title="Outlined Card" subtitle="Minimal style" />
                <AnimatedCardBody>
                  <p>This card uses borders instead of shadows for a lighter look.</p>
                </AnimatedCardBody>
                <AnimatedCardFooter>
                  <AnimatedButton size="sm" variant="primary">
                    Get Started
                  </AnimatedButton>
                </AnimatedCardFooter>
              </AnimatedCard>

              <AnimatedCard variant="filled" interactive>
                <AnimatedCardHeader title="Filled Card" subtitle="Subtle background" />
                <AnimatedCardBody>
                  <p>This card has a filled background for better contrast.</p>
                </AnimatedCardBody>
                <AnimatedCardFooter>
                  <AnimatedButton size="sm" variant="secondary">
                    Explore
                  </AnimatedButton>
                </AnimatedCardFooter>
              </AnimatedCard>
            </div>
          </div>
        </SlideIn>

        {/* Loading States */}
        <SlideIn direction="fromRight" delay={0.3}>
          <AnimatedCard>
            <AnimatedCardHeader
              title="Loading States"
              subtitle="Various loading indicators and skeletons"
            />
            <AnimatedCardBody>
              <div className="space-y-8">
                <div>
                  <h3 className="text-lg font-semibold mb-4">Spinners</h3>
                  <div className="flex items-center gap-8">
                    <LoadingSpinner size="sm" />
                    <LoadingSpinner size="md" />
                    <LoadingSpinner size="lg" />
                    <LoadingSpinner size="xl" />
                  </div>
                </div>

                <div>
                  <h3 className="text-lg font-semibold mb-4">Dots</h3>
                  <div className="flex items-center gap-8">
                    <LoadingDots size="sm" />
                    <LoadingDots size="md" />
                    <LoadingDots size="lg" />
                  </div>
                </div>

                <div>
                  <h3 className="text-lg font-semibold mb-4">Progress Bars</h3>
                  <div className="space-y-4">
                    <LoadingBar indeterminate />
                    <LoadingBar progress={progress} />
                    <div className="flex gap-2">
                      <AnimatedButton
                        size="sm"
                        variant="outline"
                        onClick={() => setProgress(Math.max(0, progress - 10))}
                      >
                        -10%
                      </AnimatedButton>
                      <AnimatedButton
                        size="sm"
                        variant="outline"
                        onClick={() => setProgress(Math.min(100, progress + 10))}
                      >
                        +10%
                      </AnimatedButton>
                    </div>
                  </div>
                </div>

                <div>
                  <h3 className="text-lg font-semibold mb-4">Skeleton Loaders</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div>
                      <h4 className="text-sm font-medium mb-2">Text Skeleton</h4>
                      <SkeletonText lines={4} />
                    </div>
                    <div>
                      <h4 className="text-sm font-medium mb-2">List Skeleton</h4>
                      <SkeletonList count={3} />
                    </div>
                  </div>
                </div>
              </div>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Feedback Animations */}
        <SlideIn direction="fromLeft" delay={0.4}>
          <AnimatedCard>
            <AnimatedCardHeader
              title="Feedback Animations"
              subtitle="Success, error, and progress indicators"
            />
            <AnimatedCardBody>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                <div className="space-y-4">
                  <h3 className="text-lg font-semibold">Success</h3>
                  <div className="flex flex-col items-center py-8 bg-gray-50 dark:bg-gray-900 rounded-lg">
                    <SuccessAnimation show={showSuccess} message="Operation completed!" />
                    {!showSuccess && (
                      <AnimatedButton
                        onClick={() => {
                          setShowSuccess(true);
                          setTimeout(() => setShowSuccess(false), 2000);
                        }}
                        leftIcon={<CheckmarkRegular />}
                      >
                        Show Success
                      </AnimatedButton>
                    )}
                  </div>
                </div>

                <div className="space-y-4">
                  <h3 className="text-lg font-semibold">Error</h3>
                  <div className="flex flex-col items-center py-8 bg-gray-50 dark:bg-gray-900 rounded-lg">
                    <ErrorAnimation
                      show={showError}
                      message="Something went wrong!"
                      onDismiss={() => setShowError(false)}
                    />
                    {!showError && (
                      <AnimatedButton
                        variant="danger"
                        onClick={() => setShowError(true)}
                        leftIcon={<DismissRegular />}
                      >
                        Show Error
                      </AnimatedButton>
                    )}
                  </div>
                </div>

                <div className="space-y-4 md:col-span-2">
                  <h3 className="text-lg font-semibold">Progress Indicators</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <ProgressIndicator
                      progress={progress}
                      label="Bar Progress"
                      variant="bar"
                      size="md"
                    />
                    <ProgressIndicator
                      progress={progress}
                      label="Circle Progress"
                      variant="circle"
                      size="md"
                    />
                  </div>
                </div>
              </div>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Tooltips */}
        <SlideIn direction="fromRight" delay={0.5}>
          <AnimatedCard>
            <AnimatedCardHeader
              title="Tooltips"
              subtitle="Smooth hover tooltips with auto-positioning"
            />
            <AnimatedCardBody>
              <div className="flex flex-wrap items-center justify-center gap-8 py-8">
                <AnimatedTooltip content="Top tooltip" placement="top">
                  <AnimatedButton variant="outline">Hover me (Top)</AnimatedButton>
                </AnimatedTooltip>
                <AnimatedTooltip content="Bottom tooltip" placement="bottom">
                  <AnimatedButton variant="outline">Hover me (Bottom)</AnimatedButton>
                </AnimatedTooltip>
                <AnimatedTooltip content="Left tooltip" placement="left">
                  <AnimatedButton variant="outline">Hover me (Left)</AnimatedButton>
                </AnimatedTooltip>
                <AnimatedTooltip content="Right tooltip" placement="right">
                  <AnimatedButton variant="outline">Hover me (Right)</AnimatedButton>
                </AnimatedTooltip>
              </div>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Staggered List */}
        <SlideIn direction="fromLeft" delay={0.6}>
          <AnimatedCard>
            <AnimatedCardHeader
              title="Staggered List Animation"
              subtitle="Items animate in sequence"
            />
            <AnimatedCardBody>
              <AnimatedList as="ul" className="space-y-3">
                {[1, 2, 3, 4, 5].map((item) => (
                  <AnimatedListItem
                    key={item}
                    as="li"
                    className="flex items-center gap-3 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg"
                  >
                    <div className="w-10 h-10 rounded-full bg-primary-500 flex items-center justify-center text-white font-semibold">
                      {item}
                    </div>
                    <div className="flex-1">
                      <h4 className="font-semibold">List Item {item}</h4>
                      <p className="text-sm text-gray-600 dark:text-gray-400">
                        This item animates with a stagger effect
                      </p>
                    </div>
                  </AnimatedListItem>
                ))}
              </AnimatedList>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Modal Demo */}
        <SlideIn direction="fromRight" delay={0.7}>
          <AnimatedCard>
            <AnimatedCardHeader title="Modal" subtitle="Smooth modal with backdrop animation" />
            <AnimatedCardBody>
              <AnimatedButton onClick={() => setShowModal(true)}>Open Modal</AnimatedButton>
            </AnimatedCardBody>
          </AnimatedCard>
        </SlideIn>

        {/* Footer */}
        <FadeIn delay={0.8}>
          <div className="text-center py-8 text-gray-600 dark:text-gray-400">
            <p>All animations respect user's reduced motion preferences</p>
            <p className="text-sm mt-2">Built with Framer Motion & Tailwind CSS</p>
          </div>
        </FadeIn>
      </div>

      {/* Modal */}
      <AnimatedModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title="Animated Modal"
        size="md"
      >
        <div className="space-y-4">
          <p className="text-gray-700 dark:text-gray-300">
            This modal has smooth enter and exit animations with a backdrop effect.
          </p>
          <p className="text-gray-700 dark:text-gray-300">
            It supports keyboard navigation (ESC to close) and click outside to dismiss.
          </p>
        </div>
        <AnimatedModalFooter>
          <AnimatedButton variant="outline" onClick={() => setShowModal(false)}>
            Cancel
          </AnimatedButton>
          <AnimatedButton onClick={() => setShowModal(false)}>Confirm</AnimatedButton>
        </AnimatedModalFooter>
      </AnimatedModal>
    </div>
  );
}
