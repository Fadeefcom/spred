import { PATH } from "@/constants/paths.ts";
import AccessDemoButton from "@/components/landing/AccessDemoButton.tsx";
import {ArrowRight} from "lucide-react";

const MinimalFooter = () => {
  const currentYear = new Date().getFullYear();

  return (
      <footer className="bg-spred-black text-spred-white pt-16 md:pt-20 pb-10 px-4 md:px-8">
        <div className="max-w-7xl mx-auto">
          <div className="flex justify-between gap-10 mb-12">
            {/* Branding & Description */}
            <div className="lg:col-span-2">
              <div className="font-display font-bold text-3xl tracking-tight mb-4">
                Spred
              </div>
              <p className="text-spred-white/80 mb-6 max-w-md">
                Connecting musicians with industry opportunities through intelligent AI matching. Find the perfect opportunities for your music career.
              </p>
              <AccessDemoButton>
                Try Demo
                <ArrowRight className="ml-2 w-4 h-4" />
              </AccessDemoButton>
            </div>

            {/* Contact */}
            <div>
              <h4 className="font-mono text-sm uppercase mb-6">Contact</h4>
              <ul className="space-y-4">
                <li>
                  <a href="mailto:hello@spred.io" className="text-spred-white/60 transition-colors animated-underline">
                    hello@spred.io
                  </a>
                </li>
                <li className="text-spred-white/60">
                  Berlin, Germany
                </li>
              </ul>

              <div className="mt-8">
                <h4 className="font-mono text-sm uppercase mb-4">Follow Us</h4>
                <div className="flex space-x-4">
                  {['telegram', 'linkedin'].map((social) => (
                      <a
                          key={social}
                          href={social === 'linkedin' ? 'https://www.linkedin.com/company/spredio' : 'https://t.me/+fNeT_rbDgU84MWQy'}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="w-10 h-10 flex items-center justify-center border border-spred-white/20 text-spred-white/60 hover:bg-spred-yellow hover:text-spred-black hover:border-transparent transition-all duration-300"
                          aria-label={social}
                      >
                        <span className="sr-only">{social}</span>
                        {social === 'telegram' && (
                            <svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24">
                              <path d="M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-1.185-.78-.417-1.21.258-1.91.177-.184 3.247-2.977 3.307-3.23.007-.032.014-.15-.056-.212s-.174-.041-.249-.024c-.106.024-1.793 1.14-5.061 3.345-.48.33-.913.49-1.302.48-.428-.008-1.252-.241-1.865-.44-.752-.245-1.349-.374-1.297-.789.027-.216.325-.437.893-.663 3.498-1.524 5.83-2.529 6.998-3.014 3.332-1.386 4.025-1.627 4.476-1.635z"/>
                            </svg>
                        )}
                        {social === 'linkedin' && (
                            <svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24">
                              <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
                            </svg>
                        )}
                      </a>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Bottom bar */}
          <div className="pt-8 border-t border-spred-white/10 flex flex-col md:flex-row justify-between items-center">
            <p className="text-sm text-spred-white/40 mb-4 md:mb-0">
              © {currentYear} Spred.io Ltd. All rights reserved.
            </p>

            <div className="flex space-x-6">
              <a href={PATH.PRIVACY_POLICY} className="text-sm text-spred-white/40 hover:text-spred-yellowdark transition-colors animated-underline">
                Privacy Policy
              </a>
              <a href={PATH.TERMS_OF_USE} className="text-sm text-spred-white/40 hover:text-spred-yellowdark transition-colors animated-underline">
                Terms of Use
              </a>
            </div>
          </div>
        </div>
      </footer>
  );
};

export default MinimalFooter;
