import { ArrowRight } from 'lucide-react';
import AnimatedSection from './AnimatedSection.tsx';
import { useNavigate } from 'react-router-dom';
import AccessDemoButton from "@/components/landing/AccessDemoButton.tsx";

const Hero = () => {
  const navigate = useNavigate();
  return (
    <section id="hero" className="relative min-h-screen flex items-center pt-32 pb-16 md:pt-40 md:pb-24 px-4 md:px-8 bg-spred-black text-spred-white overflow-hidden">
      <div className="max-w-7xl mx-auto w-full grid grid-cols-1 md:grid-cols-2 gap-12 md:gap-0 items-center relative">
        {/* Left column - text content */}
        <div className="space-y-8 pr-0 md:pr-12">
          <AnimatedSection animation="fade-up" delay={200}>
            <div className="mb-4">
              <span className="font-mono">AI-POWERED </span>
            </div>
          </AnimatedSection>
          
          <AnimatedSection animation="fade-up" delay={400}>
            <h1 className="font-display font-bold text-5xl md:text-6xl lg:text-7xl tracking-tight leading-none">
              Get your music discovered
            </h1>
          </AnimatedSection>
          
          <AnimatedSection animation="fade-up" delay={400}>
            <p className="text-lg md:text-xl text-spred-white/80 max-w-2xl">
            Spred connects musicians with perfect opportunities using cutting-edge technologies. From playlist placements to label submissions, collaborations and gigs.
            </p>
          </AnimatedSection>
          
          <AnimatedSection animation="fade-up" delay={400}>
            <div className="flex flex-col sm:flex-row gap-6 mt-8">
              <AccessDemoButton>
                Try the platform
                <ArrowRight className="ml-2 w-4 h-4" />
              </AccessDemoButton>
              
              <button
                onClick={() => {
                  window.gtag?.('event', 'learn_more_scroll_click', {
                    section: 'hero_cta',
                  });
                  const whatWeDo = document.getElementById('what-we-do');
                  if (whatWeDo) {
                    const yOffset = -80;
                    const y = whatWeDo.getBoundingClientRect().top + window.pageYOffset + yOffset;
                    window.scrollTo({ top: y, behavior: 'smooth' });
                  }
                }}
                className="spred-button-outline border-spred-white text-spred-white"
              >
                Learn More
              </button>
            </div>
          </AnimatedSection>
        </div>
        
        {/* Right column - visual */}
        <AnimatedSection animation="fade-left" delay={600} className="relative flex items-center justify-center h-full">
          <div className="w-full aspect-square relative overflow-visible">
            {/* Main hero image or graphic */}
            <div className="absolute inset-0 flex items-center justify-center">
              <img 
                src="/images/landing-image-1.jpg"
                alt="Music platform interface" 
                className="w-full h-auto object-cover"
              />
            </div>
            
            {/* Decorative elements */}
            <div className="absolute bottom-10 right-6 w-1/6 h-1/6 bg-spred-yellow transform translate-x-1/6 translate-y-1/6"></div>
            <div className="absolute top-0 left-0 w-1/4 h-1/4 border-2 border-spred-mint-green transform -translate-x-1/4 -translate-y-1/4"></div>
            <div className="absolute top-1/3 right-0 w-16 h-16 rounded-full bg-spred-teal transform translate-x-1/2"></div>
          </div>
        </AnimatedSection>
      </div>
    </section>
  );
};

export default Hero;
