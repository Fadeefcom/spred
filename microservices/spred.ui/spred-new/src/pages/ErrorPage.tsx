import React from "react"

interface ErrorPageProps {
    title?: string
    detail?: string
    status?: string
}

const ErrorPage = ({
                       title = "Page Not Found",
                       detail = "The page you are looking for does not exist.",
                       status = "404",
                   }: ErrorPageProps) => {
    return (
        <div className="w-full max-w-7xl mx-auto pt-6 pb-6">
            <div className="text-center">
                <h1 className="text-4xl font-bold mb-4">{title}</h1>
                <h2 className="text-4xl font-bold mb-4">{status}</h2>
                <p className="text-xl text-gray-600 mb-4">{detail}</p>
                <a href="/" className="text-blue-500 hover:text-blue-700 underline">
                    Return to Home
                </a>
            </div>
        </div>
    )
}

export default ErrorPage