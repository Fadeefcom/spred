import { ArrowRight, Search, Sparkles, Music, Heart } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import AnimatedSection from './AnimatedSection';
import AccessDemoButton from "@/components/landing/AccessDemoButton.tsx";

const WhatWeDo = () => {
  const features = [
    {
      icon: <Search className="w-6 h-6" />,
      title: "Smart Industry Matches",
      description: "Our AI technology analyzes thousands of opportunities to find perfect matches for your music style, skills, and career goals."
    },
    {
      icon: <Sparkles className="w-6 h-6" />,
      title: "Zero-Effort Process",
      description: "Pitch to playlists and sync opportunities in seconds—no more spreadsheets or cold emails."
    },
    {
      icon: <Music className="w-6 h-6" />,
      title: "Music-First Approach",
      description: "Our platform understands your music on a deeper level, going beyond basic genre classifications to find truly meaningful matches."
    },
    {
      icon: <Heart className="w-6 h-6" />,
      title: "Industry Connections",
      description: "Get connected with labels, promoters, sync agencies, and other musicians looking for your exact talent and sound."
    }
  ];

  const navigate = useNavigate();

  return (
      <section id="what-we-do" className="section relative bg-spred-light-gray">
        <div className="max-w-7xl mx-auto">
          {/* Section heading */}
          <AnimatedSection animation="fade-up" className="max-w-3xl mx-auto mb-16 md:mb-24">
            <div id="how-it-works" className="mb-4">
              <span className="font-mono text-spred-black">HOW IT WORKS</span>
            </div>
            <h2 className="text-4xl md:text-5xl lg:text-6xl font-display font-bold mb-6 text-spred-black">
              How <span className="text-spred-yellow bg-spred-black px-1">Spred</span> Works
            </h2>
            <p className="text-lg md:text-xl text-spred-dark-gray">
              We use machine learning to bridge the gap between artists and industry professionals.
            </p>
          </AnimatedSection>

          {/* Features grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 md:gap-12">
            {features.map((feature, index) => (
                <AnimatedSection
                    key={index}
                    animation="fade-up"
                    delay={(index % 2 === 0) ? 200 : 400}
                    className="bg-spred-white text-spred-black p-8 md:p-10 border border-spred-black/10 transition-all duration-300 hover:border-spred-black"
                >
                  <div className="w-14 h-14 flex items-center justify-center mb-6 bg-spred-black text-spred-yellow">
                    {feature.icon}
                  </div>
                  <h3 className="text-xl md:text-2xl font-display font-semibold mb-4">
                    {feature.title}
                  </h3>
                  <p className="text-spred-dark-gray mb-6">
                    {feature.description}
                  </p>
                </AnimatedSection>
            ))}
          </div>

          {/* Platform preview */}
          <div id="try-platform"></div>
          <AnimatedSection
              animation="fade-up"
              className="mt-20 md:mt-32 border border-spred-black overflow-hidden"
          >
            <div className="p-8 md:p-10 grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
              <div className="space-y-6 text-spred-black">
                <div className="mb-4">
                  <span className="font-mono text-spred-black">AI-POWERED PLATFORM</span>
                </div>

                <h3 className="text-2xl md:text-3xl font-display font-bold">
                  Designed for the modern musician's workflow
                </h3>

                <p className="text-spred-dark-gray">
                  Our platform is built for musicians at its core. We understand the unique challenges of finding quality opportunities in today's music industry, and we've designed a system that makes the process seamless and effective.
                </p>

                <ul className="space-y-3 mt-8">
                  {[
                    "Built by artists and tech lovers",
                    "Designed for the modern, global music scene",
                    "Scalable for every stage of your journey",
                    "Backed by deep industry research and AI innovation"
                  ].map((item, index) => (
                      <li key={index} className="flex items-start">
                        <div className="w-5 h-5 flex-shrink-0 flex items-center justify-center text-spred-black mt-1 mr-3">
                          <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path d="M10 3L4.5 8.5L2 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                          </svg>
                        </div>
                        <span>{item}</span>
                      </li>
                  ))}
                </ul>

                <AccessDemoButton>
                  Try the platform
                  <ArrowRight className="ml-2 w-4 h-4" />
                </AccessDemoButton>
              </div>

              <div className="relative rounded-none overflow-hidden aspect-[4/3]">
                <img
                    src="/images/screenshot.png"
                    alt="Platform Preview"
                    className="w-full h-full object-cover"
                />
              </div>
            </div>
          </AnimatedSection>
        </div>
      </section>
  );
};

export default WhatWeDo; 