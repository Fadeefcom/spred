import React, { useState } from 'react';
import { Send } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import Select from '@/components/ui/select';
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {useToast} from "@/hooks/use-toast.ts";
import {SERVICES} from "@/constants/services.tsx";
import {useTheme} from "@/components/theme/useTheme.ts";

const Feedback: React.FC = () => {
  const [feedbackType, setFeedbackType] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');
  const { toast } = useToast();
  const { resolvedTheme } = useTheme();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const payload = {
      feedbackType,
      subject,
      message
    };

    try {
      setIsSubmitting(true);
      const response = await apiFetch(`${SERVICES.USER}/user/feedback`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast({
          title: '✅ Success!',
          description: 'Your feedback has been sent. Thank you!',
          variant: 'default',
        });

        setFeedbackType('');
        setSubject('');
        setMessage('');
      } else {
        toast({
          title: '⚠️ Error',
          description: 'Failed to send feedback. Please try again later.',
          variant: 'destructive',
        });
        console.error('Failed to send feedback');
      }
    } catch (error) {
      toast({
        title: '❌ Network error',
        description: 'Something went wrong while sending your feedback.',
        variant: 'destructive',
      });
      console.error('Error sending feedback:', error);
    }
    finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-[calc(100vh-128px)]">
      <div className="max-w-3xl mx-auto px-4 md:px-6 pt-8 md:pt-12">
        <div className="animate-fade-in">
          <h1 className="text-3xl md:text-4xl font-bold mb-2">Feedback</h1>
          <p className="text-muted-foreground mb-8">
            We value your feedback! Let us know how we can improve your experience.
          </p>

          <div className="bg-accent/50 p-6">
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-2">
                <label htmlFor="feedbackType" className="block text-sm font-medium">
                  Feedback Type
                </label>
                <Select
                    id="feedbackType"
                    name="feedbackType"
                    required
                    value={feedbackType}
                    onChange={(e) =>
                        setFeedbackType(e.target.value)
                    }
                    placeholder="Select feedback type"
                    theme={resolvedTheme}
                    options={[
                      { value: "bug", label: "Bug Report" },
                      { value: "feature", label: "Feature Request" },
                      { value: "improvement", label: "Improvement Suggestion" },
                      { value: "other", label: "Other" },
                    ]}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="subject" className="block text-sm font-medium">
                  Subject
                </label>
                <Input
                  id="subject"
                  required
                  placeholder="Brief description of your feedback"
                  theme={resolvedTheme}
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="message" className="block text-sm font-medium">
                  Message
                </label>
                <Textarea
                  id="message"
                  placeholder="Please provide detailed feedback..."
                  theme={resolvedTheme}
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  className="min-h-[150px]"
                />
              </div>

              <Button type="submit" className="w-full md:w-auto">
                {isSubmitting ? (
                        <span className="flex items-center justify-center">
                          <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                          </svg>
                          Processing...
                        </span>
                    ) : (
                    <>
                      <Send className="w-4 h-4 mr-2" />
                      Submit Feedback
                    </>)
                }
              </Button>
            </form>
          </div>

          <p className="text-muted-foreground mt-8 text-center">
            You can also reach out to us directly at{' '}
            <a
              href="mailto:hello@spred.io"
              className="active-link transition-colors animated-underline"
            >
              hello@spred.io
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Feedback; 