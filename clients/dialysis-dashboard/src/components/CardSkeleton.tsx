export function CardSkeleton() {
    return (
        <div className="p-5 border border-gray-200 rounded-lg bg-white shadow-sm animate-pulse">
            <div className="h-5 w-32 bg-gray-200 rounded mb-4" />
            <div className="h-4 w-full bg-gray-200 rounded my-2" />
            <div className="h-4 w-3/4 bg-gray-200 rounded my-2" />
            <div className="h-3 w-24 bg-gray-200 rounded mt-4" />
        </div>
    );
}
