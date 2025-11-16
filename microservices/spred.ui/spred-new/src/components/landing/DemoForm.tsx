import React, { useState, useRef } from 'react';
import { Check, ArrowRight } from 'lucide-react';
import AnimatedSection from './AnimatedSection.tsx';
import { useToast } from '@/hooks/use-toast.ts';
import {SERVICES} from "@/constants/services.tsx";
import {Input} from "@/components/ui/input.tsx";
import Select from "@/components/ui/select.tsx";
import {Textarea} from "@/components/ui/textarea.tsx";

const DemoForm = () => {
  const { toast } = useToast();
  const hasStartedForm = useRef(false);
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    artistType: '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await fetch(`${SERVICES.USER}/user/notify`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      if(response.ok){
        window.gtag?.('event', 'form_submit', {
          form_id: 'demo_early_access',
          location: 'demo_section',
          email: formData.email,
          role: formData.artistType,
        });

        toast({
          title: "Success!",
          description: "You've been added to our early access list.",
        });
        setIsSubmitted(true);

        setFormData({
          name: '',
          email: '',
          artistType: '',
          message: '',
        });
      }
      else{
        toast({
          title: "Failed",
          description: "There was a problem with request. Try again.",
        });
        setIsSubmitted(false);
      }

    }
    finally {
      setIsSubmitting(false);
    }
  };

  const root = document.documentElement;
  root.classList.add("light");

  return (
    <section className="section relative bg-white text-black">
      <div className="max-w-7xl mx-auto">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-20 items-center">
          {/* Form side */}
          <AnimatedSection animation="fade-right" className="order-2 lg:order-1">
            <div id="demo-form" className="glass-card p-8 md:p-10">
              {isSubmitted ? (
                <div className="text-center py-8">
                  <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6 animate-fade-in">
                    <Check className="w-8 h-8 text-green-600" />
                  </div>
                  <h3 className="text-2xl font-display font-bold mb-4">Thanks for your interest!</h3>
                  <p className="text-gray-700 mb-6">
                    We've added you to our early access list. We'll reach out soon with more details on how to access the platform.
                  </p>
                  <button
                    onClick={() => setIsSubmitted(false)}
                    className="spred-button"
                  >
                    Submit Another Request
                  </button>
                </div>
              ) : (
                <>
                  <h3 className="text-2xl md:text-3xl font-display font-bold mb-6">
                    Don't miss out
                  </h3>
                  <p className="text-gray-700 mb-8">
                    Sign up to join our beta, know when we launch, and stay updated on new features. Be the first to know how Spred is changing music promotion.
                  </p>
                  
                  <form onSubmit={handleSubmit} className="space-y-6">
                    <div className="space-y-4">
                      <div>
                        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                          Your Name
                        </label>
                        <Input
                          type="text"
                          id="name"
                          name="name"
                          value={formData.name}
                          onChange={handleChange}
                          required
                          placeholder="Enter your name"
                          theme="light"
                          onFocus={() => {
                            if (!hasStartedForm.current) {
                              window.gtag?.('event', 'form_start', {
                                form_id: 'demo_early_access',
                                location: 'demo_section',
                              });
                              hasStartedForm.current = true;
                            }
                          }}
                        />
                      </div>
                      
                      <div>
                        <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                          Email Address
                        </label>
                        <Input
                          type="email"
                          id="email"
                          name="email"
                          value={formData.email}
                          onChange={handleChange}
                          theme="light"
                          required
                          placeholder="you@example.com"
                        />
                      </div>
                      
                      <div>
                        <label htmlFor="artistType" className="block text-sm font-medium text-gray-700 mb-1">
                        Your Role in the Music Industry
                        </label>
                        <Select
                            id="artistType"
                            name="artistType"
                            required
                            value={formData.artistType}
                            onChange={handleChange}
                            placeholder="Select your primary role"
                            theme="light"
                            options={[
                              { value: "artist", label: "Artist/Producer" },
                              { value: "sync", label: "Sync Agency" },
                              { value: "distributor", label: "Distributor" },
                              { value: "label", label: "Label Representative" },
                              { value: "dj", label: "DJ" },
                              { value: "curator", label: "Music Curator" },
                              { value: "booking", label: "Booking Agent" },
                              { value: "other", label: "Other" },
                            ]}
                        />
                      </div>
                      
                      <div>
                        <label htmlFor="message" className="block text-sm font-medium text-gray-700 mb-1">
                          What are you looking for? (optional)
                        </label>
                        <Textarea
                          id="message"
                          name="message"
                          value={formData.message}
                          onChange={handleChange}
                          theme="light"
                          rows={4}
                          placeholder="Tell us about what opportunities you're interested in..."
                          />
                      </div>
                    </div>
                    
                    <button
                      type="submit"
                      disabled={isSubmitting}
                      className="spred-button w-full"
                    >
                      {isSubmitting ? (
                        <span className="flex items-center justify-center">
                          <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                          </svg>
                          Processing...
                        </span>
                      ) : (
                        <span className="flex items-center justify-center">
                          Request Early Access
                          <ArrowRight className="ml-2 w-5 h-5" />
                        </span>
                      )}
                    </button>
                  </form>
                </>
              )}
            </div>
          </AnimatedSection>
          
          {/* Content side */}
          <AnimatedSection animation="fade-left" className="order-1 lg:order-2">
            <h2 className="text-3xl md:text-4xl lg:text-5xl font-display font-bold mb-6">
              Join the future of <span className="text-gradient">music promotion</span>
            </h2>
            <p className="text-lg text-gray-700 mb-8">
            We're inviting music industry professionals to experience our beta platform. Be among the first to try our AI-powered artist matching and give us feedback to shape the future of Spred.
            </p>
            
            <div className="space-y-6">
              {[
                {
                  title: "Get Personalised Recommendations",
                  description: "Spred analyzes artists and their music to connect you with creators that perfectly fit your label, playlist, or industry needs."
                },
                {
                  title: "Shape the Platform",
                  description: "Your feedback will directly influence how we develop the platform and what features we prioritize."
                },
                {
                  title: "Connect with our Community",
                  description: "Early users will have direct access to our team through our Telegram community for support."
                }
              ].map((benefit, index) => (
                <div key={index} className="flex items-start">
                  <div className="w-6 h-6 rounded-full bg-gradient-to-br from-spred-blue to-spred-purple flex-shrink-0 flex items-center justify-center text-white mt-1 mr-4">
                    <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M10 3L4.5 8.5L2 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                  </div>
                  <div>
                    <h4 className="text-lg font-semibold mb-1 text-spred-black">{benefit.title}</h4>
                    <p className="text-gray-700">{benefit.description}</p>
                  </div>
                </div>
              ))}
            </div>
          </AnimatedSection>
        </div>
      </div>
    </section>
  );
};

export default DemoForm;
