import React from "react";

const PrivacyPolicy = () => {
    return (
        <div className="w-full max-w-7xl mx-auto pt-6 pb-6">
            <h1 className="mb-4">Spred: Privacy Policy</h1>
            <p><strong>Effective Date:</strong> August 14th, 2025</p>
            <p><strong>Last Updated:</strong> August 14th, 2025</p>

            <h2 className="mt-6 mb-2">Introduction</h2>
            <p className="mb-4">
                Welcome to Spred.io ("Spred," "we," "our," or "us"). We value your privacy and are committed to
                protecting your personal information. This Privacy Policy explains how we collect, use, share, and
                protect your information. It is designed to comply with global privacy laws, including the General
                Data Protection Regulation (GDPR), the UK GDPR, and the California Consumer Privacy Act (CCPA/CPRA).
                By using our services, you agree to the terms of this Privacy Policy.
            </p>

            <h2 className="mt-6 mb-2">Information We Collect</h2>
            <ul className="list-disc list-inside mb-4">
                <li><strong>Personal Information:</strong> Name, email, phone number, payment details. Payment data is processed securely through third-party payment processors.</li>
                <li><strong>Usage Data:</strong> IP address, device type, browser type, and interactions with the platform.</li>
                <li><strong>Music Uploads:</strong> Audio files you upload, which we process to generate embeddings and recommendations. We retain original music files to improve our AI-based music analysis models (for recommendation purposes only) but we do not use them for generative AI, nor do we share, sell, or distribute the uploaded music.</li>
                <li><strong>Cookies and Analytics:</strong> We use cookies and Google Analytics to understand user behavior, monitor traffic, and improve services.</li>
                <li><strong>AI-Based Products:</strong> Our AI analyzes your uploads and interactions to personalize recommendations.</li>
            </ul>
            <p className="mb-4">We do not knowingly collect data from children under 18.</p>

            <h2 className="mt-6 mb-2">Music Uploads</h2>
            <p className="mb-2">Your uploaded music files are treated as both personal data (where applicable) and your intellectual property. We process these files solely to operate and improve the services you have chosen to use on the platform.</p>
            <p className="mb-2">We use Your Music to:</p>
            <ul className="list-disc list-inside mb-4">
                <li>Generate audio embeddings and analyses to provide recommendations and promotion tools.</li>
                <li>Support your use of platform features such as playlist suggestions, label introductions, or performance analytics.</li>
                <li>Maintain a secure backup for your access while your account is active.</li>
            </ul>
            <p className="mb-2">We do not:</p>
            <ul className="list-disc list-inside mb-4">
                <li>Sell or license Your Music to any third party without your explicit consent.</li>
                <li>Use Your Music for generative AI, training unrelated models, or creating derivative works for our own commercial use.</li>
                <li>Make Your Music publicly available outside the scope you have selected.</li>
            </ul>
            <p className="mb-4">
                If you close your account, we will securely delete Your Music from our servers within 60 days unless retention is legally required (e.g., for dispute resolution).
            </p>

            <h2 className="mt-6 mb-2">Legal Basis for Processing (GDPR/UK GDPR)</h2>
            <ul className="list-disc list-inside mb-4">
                <li><strong>Consent:</strong> For marketing communications, non-essential cookies, and promotional outreach.</li>
                <li><strong>Contract:</strong> To deliver our services and manage your account.</li>
                <li><strong>Legitimate Interests:</strong> To improve services, conduct analytics, enhance music recommendations, and ensure platform security.</li>
                <li><strong>Legal Obligations:</strong> To comply with applicable laws and regulations.</li>
            </ul>

            <h2 className="mt-6 mb-2">How We Use Your Information</h2>
            <ul className="list-disc list-inside mb-4">
                <li>Provide and maintain the platform.</li>
                <li>Analyze your uploaded music to improve AI recommendations (without using it for generative AI or external distribution).</li>
                <li>Personalize recommendations.</li>
                <li>Communicate service updates, security alerts, and marketing (with opt-out options).</li>
                <li>Analyze usage for platform improvements.</li>
                <li>Meet legal obligations and protect our rights.</li>
            </ul>
            <p className="mb-4">We do not engage in automated decision-making that produces significant legal or similar effects on you.</p>

            <h2 className="mt-6 mb-2">Cookies and Tracking Technologies</h2>
            <p className="mb-4">
                We use essential, functional, and analytics cookies. Non-essential cookies (e.g., for marketing or personalization) require your consent.
                You can review and manage your cookie preferences via our Cookie Banner or by adjusting your browser settings.
                For detailed information, see our Cookie Policy.
            </p>

            <h2 className="mt-6 mb-2">Children’s Privacy and Parental Consent</h2>
            <p className="mb-2">
                We do not knowingly collect, store, or process personal data from individuals under the age of 18 without appropriate parental or guardian consent.
                If we discover that we have collected personal information from a minor without such consent, we will delete it promptly.
            </p>
            <p className="mb-2"><strong>Parental Rights:</strong> Parents or guardians can review, update, or request deletion of their child’s data via <a href="mailto:hello@spred.io" className="text-spred-yellowdark">hello@spred.io</a>.</p>
            <p className="mb-2"><strong>Data Minimization for Minors:</strong> If parental consent is obtained, we collect only minimal data necessary for service provision and apply enhanced protections.</p>
            <ul className="list-disc list-inside mb-4">
                <li><strong>COPPA (US):</strong> Verifiable parental consent for children under 13.</li>
                <li><strong>UK Children’s Code:</strong> We design our services with children's safety in mind.</li>
                <li><strong>GDPR Article 8 (EU):</strong> We respect member-state consent ages (13–16).</li>
            </ul>
            <p className="mb-4">We do not serve targeted advertising or enable unsolicited contact for minors.</p>

            <h2 className="mt-6 mb-2">Data Sharing and Disclosure</h2>
            <ul className="list-disc list-inside mb-4">
                <li>With service providers (e.g., hosting, analytics, customer support) under confidentiality agreements.</li>
                <li>For legal compliance (e.g., law enforcement requests, court orders).</li>
                <li>In business transfers (e.g., mergers, acquisitions, or asset sales).</li>
                <li>With third-party integrations (e.g., embedded players), subject to their privacy policies.</li>
            </ul>
            <p className="mb-4">We do not sell, share, or distribute your uploaded music files to third parties.</p>

            <h2 className="mt-6 mb-2">International Data Transfers</h2>
            <p className="mb-4">
                If data is transferred outside the EU/UK, we use standard contractual clauses or other approved safeguards to ensure adequate protection.
            </p>

            <h2 className="mt-6 mb-2">Your Rights</h2>
            <ul className="list-disc list-inside mb-4">
                <li>Access your personal data.</li>
                <li>Request correction or deletion.</li>
                <li>Object to processing (including marketing).</li>
                <li>Request data portability.</li>
            </ul>
            <p className="mb-4">
                California residents have additional rights under CCPA/CPRA, including the right to opt out of sale or sharing of personal information.
            </p>
            <p className="mb-4">
                To exercise your rights, contact us at <a href="mailto:hello@spred.io" className="text-spred-yellowdark">hello@spred.io</a>.
            </p>

            <h2 className="mt-6 mb-2">Data Retention</h2>
            <p className="mb-4">
                We retain your data only as long as necessary for the purposes described or as required by law.
                After account deletion, we securely delete or anonymize personal data and remove music uploads within 60 days.
            </p>

            <h2 className="mt-6 mb-2">Security Measures</h2>
            <p className="mb-4">
                We use encryption, secure servers, and regular audits to protect your information from unauthorized access, loss, or misuse.
            </p>

            <h2 className="mt-6 mb-2">Data Protection Officer and EU/UK Representative</h2>
            <p className="mb-4">
                For GDPR inquiries, contact our Data Protection Officer (DPO): <a href="mailto:dpo@spred.io" className="text-spred-yellowdark">dpo@spred.io</a>.
            </p>

            <h2 className="mt-6 mb-2">Updates to This Policy</h2>
            <p className="mb-4">
                We may update this Privacy Policy periodically. Significant updates will be communicated via email or our website.
            </p>

            <h2 className="mt-6 mb-2">Contact Us</h2>
            <p className="mb-4">
                If you have any questions or concerns, contact us at <a href="mailto:hello@spred.io" className="text-spred-yellowdark">hello@spred.io</a>.
            </p>

            <h2 className="mt-6 mb-2">Do Not Sell or Share My Personal Information (California Residents)</h2>
            <p className="mb-4">
                California residents can exercise opt-out rights via our “Do Not Sell or Share My Personal Information” page.
            </p>

            <p className="mt-6">This Privacy Policy was last updated on August 14th, 2025.</p>
        </div>
    );
};

export default PrivacyPolicy;
