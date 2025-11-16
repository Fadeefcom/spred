import {Toaster} from "@/components/ui/toaster";
import {TooltipProvider} from "@/components/ui/tooltip";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter,  Outlet, Route, Routes,} from "react-router-dom";
import {PATH} from '@/constants/paths.ts';
import Lending from "./pages/Lending.tsx";
import PrivacyPolicy from "./pages/PrivacyPolicy";
import ErrorPage from "./pages/ErrorPage";
import {Loading} from "@/components/loading/loading.tsx"
import React, {Suspense} from "react";
import Dashboard from "./pages/Artist/Dashboard.tsx";
import UploadTrack from "./pages/Artist/UploadTrack.tsx";
import TrackDetails from "./pages/Artist/TrackDetails.tsx";
import Feedback from "./pages/Feedback";
import { RecommendationModalProvider } from "./hooks/useRecommendationModal";
import RecommendationDetailsModal from "./components/modals/RecommendationDetailsModal";
import { ThemeProvider } from "./components/theme/theme-provider";
import {useClientHintInit} from "@/hooks/useClientHintInit.ts";
import MinimalLayout from "@/components/layout/MinimalLayout.tsx";
import {RequireAuth} from "@/components/authorization/RequireAuth.tsx"
import LoginPage from "@/pages/LoginPage.tsx";
import {AudioPlayerProvider} from "@/components/player/AudioPlayerContext.tsx";
import {CookieBanner} from "@/components/banner/CookieBanner.tsx";
import { AuthProvider } from "@/components/authorization/AuthProvider.tsx";
import {AnalyticsProvider} from "@/components/analytics/AnalyticsProvider.tsx";
import ArtistLayout from "./components/layout/ArtistLayout.tsx";
import CuratorLayout from "./components/layout/CuratorLayout.tsx";
import LabelLayout from "@/components/layout/LabelLayout.tsx";
import TermsOfUse from "@/pages/TermsOfUse.tsx";
import Profile from "@/pages/Profile.tsx";
import CuratorDashboard from "@/pages/Curator/CuratorDasboard.tsx";
import CuratorSubmissions from "@/pages/Curator/CuratorSubmissions.tsx";
import Accounts from "@/pages/Curator/Accounts.tsx";
import Upgrade from "@/pages/Upgrade.tsx";
import {UploadLimitProvider} from "@/hooks/useUploadLimit.tsx";

const queryClient = new QueryClient();


const App = () => {
    useClientHintInit();

    return (
        <QueryClientProvider client={queryClient}>
            <TooltipProvider>
                <CookieBanner />
                <Toaster />
                <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
                    <AnalyticsProvider>
                        <ThemeProvider>
                            <Suspense fallback={<Loading />}>
                                <div className="min-h-screen bg-background text-foreground">
                                    <AuthProvider>
                                        <AudioPlayerProvider>
                                            <Routes>
                                                {/* Публичные страницы */}
                                                <Route path="/" element={<Lending />} />
                                                <Route
                                                    path={PATH.LOGIN}
                                                    element={
                                                        <MinimalLayout>
                                                            <LoginPage />
                                                        </MinimalLayout>
                                                    }
                                                />
                                                <Route
                                                    path={PATH.PRIVACY_POLICY}
                                                    element={
                                                        <MinimalLayout>
                                                            <PrivacyPolicy />
                                                        </MinimalLayout>
                                                    }
                                                />
                                                <Route
                                                    path={PATH.TERMS_OF_USE}
                                                    element={
                                                        <MinimalLayout>
                                                            <TermsOfUse />
                                                        </MinimalLayout>
                                                    }
                                                />
                                                <Route
                                                    path={PATH.NOT_FOUND}
                                                    element={
                                                        <MinimalLayout>
                                                            <ErrorPage />
                                                        </MinimalLayout>
                                                    }
                                                />

                                                {/* Artist zone */}
                                                <Route
                                                    path={PATH.ARTIST.ROOT}
                                                    element={
                                                        <RequireAuth allowed={["artist"]}>
                                                            <ArtistLayout>
                                                                <Outlet />
                                                            </ArtistLayout>
                                                        </RequireAuth>
                                                    }
                                                >
                                                    <Route index element={<Dashboard />} />
                                                    <Route
                                                        path="upload"
                                                        element={
                                                            <UploadLimitProvider>
                                                                <UploadTrack />
                                                            </UploadLimitProvider>
                                                        }
                                                    />
                                                    <Route
                                                        path="tracks/:trackId"
                                                        element={
                                                            <RecommendationModalProvider>
                                                                <RecommendationDetailsModal />
                                                                <TrackDetails />
                                                            </RecommendationModalProvider>
                                                        }
                                                    />
                                                    <Route path="profile" element={<Profile />} />
                                                    <Route path="feedback" element={<Feedback />} />
                                                    <Route path="upgrade" element={<Upgrade />} />
                                                </Route>

                                                {/* Curator zone */}
                                                <Route
                                                    path={PATH.CURATOR.ROOT}
                                                    element={
                                                        <RequireAuth allowed={["curator"]}>
                                                            <CuratorLayout>
                                                                <Outlet />
                                                            </CuratorLayout>
                                                        </RequireAuth>
                                                    }
                                                >
                                                    <Route index element={<CuratorDashboard />} />
                                                    <Route path="submissions" element={<CuratorSubmissions />} />
                                                    <Route path="profile" element={<Profile />} />
                                                    <Route path="feedback" element={<Feedback />} />
                                                    <Route path="accounts" element={<Accounts />} />
                                                    <Route path="upgrade" element={<Upgrade />} />
                                                </Route>

                                                {/* Label zone */}
                                                <Route
                                                    path={PATH.LABEL.ROOT}
                                                    element={
                                                        <RequireAuth allowed={["label"]}>
                                                            <LabelLayout>
                                                                <Outlet />
                                                            </LabelLayout>
                                                        </RequireAuth>
                                                    }
                                                >
                                                    {/*<Route index element={<LabelDashboard />} />
                                                    <Route path={PATH.ARTISTS} element={<Artists />} />
                                                    <Route path={PATH.REPORTS} element={<Reports />} />*/}
                                                    <Route path="feedback" element={<Feedback />} />
                                                </Route>

                                                {/* Fallback */}
                                                <Route path="*" element={
                                                    <MinimalLayout>
                                                        <ErrorPage />
                                                    </MinimalLayout>
                                                } />
                                            </Routes>
                                        </AudioPlayerProvider>
                                    </AuthProvider>
                                </div>
                            </Suspense>
                        </ThemeProvider>
                    </AnalyticsProvider>
                </BrowserRouter>
            </TooltipProvider>
        </QueryClientProvider>
    )
}

export default App;
