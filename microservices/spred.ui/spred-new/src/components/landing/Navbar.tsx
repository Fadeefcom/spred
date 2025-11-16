import { useState, useEffect } from 'react';
import { cn } from "@/lib/utils.ts";
import {ArrowRight, Menu, X} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import AccessDemoButton from "@/components/landing/AccessDemoButton.tsx";

const Navbar = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 20);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  useEffect(() => {
    //document.body.style.overflow = isOpen ? 'hidden' : 'auto';
    return () => {
      //document.body.style.overflow = 'auto';
    };
  }, [isOpen]);

  const scrollToSection = (id: string) => {
    setIsOpen(false);
    const element = document.getElementById(id);
    if (element) {
      const yOffset = -80;
      const y = element.getBoundingClientRect().top + window.pageYOffset + yOffset;
      window.scrollTo({ top: y, behavior: 'smooth' });
    }
  };

  return (
      <header
          className={cn(
              "fixed top-0 left-0 right-0 z-50 transition-all duration-300 px-4 md:px-8 py-6",
              isScrolled ? "bg-spred-white text-spred-black" : "bg-spred-black text-spred-white"
          )}
      >
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <a
              href="#hero"
              onClick={(e) => {
                e.preventDefault();
                window.scrollTo({ top: 0, behavior: 'smooth' });
              }}
              className="flex items-center space-x-2 animate-fade-in"
          >
            <div className="font-display font-bold text-3xl tracking-tight">
              Spred
            </div>
          </a>

          {/* Desktop navigation */}
          <nav className="hidden md:flex items-center space-x-8 animate-fade-in">
            {[
              { name: 'Home', id: 'hero' },
              { name: 'How It Works', id: 'what-we-do' },
            ].map((item) => (
                <button
                    key={item.id}
                    onClick={() => {
                      window.gtag?.('event', 'header_navigation_click', {
                        link_name: item.name,
                        link_target_id: item.id,
                        page_path: window.location.pathname,
                      });
                      scrollToSection(item.id)
                    }}
                    className={cn(
                        "relative text-base font-medium animated-underline transition-colors",
                        isScrolled ? "text-spred-black" : "text-spred-white"
                    )}
                >
                  {item.name}
                </button>
            ))}
            <AccessDemoButton/>
          </nav>

          {/* Mobile menu button */}
          <button
              className="md:hidden flex items-center p-2"
              onClick={() => setIsOpen(!isOpen)}
              aria-label={isOpen ? "Close menu" : "Open menu"}
          >
            {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>
        </div>

        {/* Mobile navigation overlay */}
        <div
            className={cn(
                "fixed inset-0 bg-spred-black text-spred-white" +
                " flex flex-col p-8 pt-24 z-40 transform transition-transform duration-300 ease-in-out",
                isOpen ? "translate-x-0" : "translate-x-full"
            )}
        >
          <button
              className="absolute top-4 right-4 p-2 rounded-full hover:bg-accent"
              onClick={() => setIsOpen(false)}
              aria-label="Close menu"
          >
            <X className="w-6 h-6" />
          </button>

          <nav className="flex flex-col space-y-8">
            {[
              { name: 'Home', id: 'hero' },
              { name: 'What We Do', id: 'what-we-do' },
            ].map((item) => (
                <button
                    key={item.id}
                    onClick={() => scrollToSection(item.id)}
                    className="text-xl font-medium"
                >
                  {item.name}
                </button>
            ))}
            <button
                onClick={() => scrollToSection('demo-form')}
                className="spred-button mt-4 w-full"
            >
              Get Early Access
            </button>
          </nav>
        </div>
      </header>
  );
};

export default Navbar;
