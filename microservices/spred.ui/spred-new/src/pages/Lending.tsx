import { useEffect } from 'react';
import Navbar from '@/components/landing/Navbar.tsx';
import Hero from '@/components/landing/Hero.tsx';
import WhatWeDo from '@/components/landing/WhatWeDo.tsx';
import DemoForm from '@/components/landing/DemoForm.tsx';
import Footer from '@/components/landing/Footer.tsx';
import {Helmet} from "react-helmet";

const Lending = () => {
  useEffect(() => {
    // Apply scroll animations using Intersection Observer
    const handleIntersection = (entries: IntersectionObserverEntry[]) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('active');
        }
      });
    };

    const observer = new IntersectionObserver(handleIntersection, {
      rootMargin: '0px',
      threshold: 0.15,
    });

    // Observe all elements with reveal classes
    const revealElements = document.querySelectorAll('.reveal, .reveal-up, .reveal-left, .reveal-right');
    revealElements.forEach(el => observer.observe(el));

    // Clean up on component unmount
    return () => {
      revealElements.forEach(el => observer.unobserve(el));
    };
  }, []);

  // Scroll to top on mount
  useEffect(() => {
    window.scrollTo(0, 0);
  }, []);

  return (
      <>
          <Helmet>
              <html lang="en" />
          </Helmet>
          <div className="h-[100svh] flex flex-col overflow-y-auto w-full">
              <Navbar />
              <main className="flex-grow w-full">
                  <Hero />
                  <WhatWeDo />
                  <DemoForm />
              </main>
              <Footer />
          </div>
      </>
  );
};

export default Lending;
