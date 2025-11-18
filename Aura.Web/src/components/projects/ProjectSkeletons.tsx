import { Skeleton } from '../Loading/Skeleton';

/**
 * Skeleton loader for project cards in grid view
 * Matches the ProjectCard layout
 */
export function ProjectCardSkeleton() {
  return (
    <div className="relative bg-white dark:bg-gray-800 rounded-lg border-2 border-gray-200 dark:border-gray-700 p-4">
      {/* Thumbnail */}
      <Skeleton variant="rectangular" height="160px" className="mb-4" />

      {/* Title */}
      <Skeleton variant="text" height="24px" width="80%" className="mb-2" />

      {/* Description */}
      <Skeleton variant="text" height="16px" width="100%" className="mb-1" />
      <Skeleton variant="text" height="16px" width="90%" className="mb-3" />

      {/* Tags */}
      <div className="flex gap-2 mb-3">
        <Skeleton variant="rounded" height="24px" width="60px" />
        <Skeleton variant="rounded" height="24px" width="80px" />
      </div>

      {/* Footer meta */}
      <div className="flex items-center justify-between">
        <Skeleton variant="text" height="14px" width="100px" />
        <Skeleton variant="text" height="14px" width="80px" />
      </div>
    </div>
  );
}

/**
 * Skeleton loader for project list items in table view
 * Matches the ProjectListItem layout
 */
export function ProjectListItemSkeleton() {
  return (
    <tr className="border-b border-gray-200 dark:border-gray-700">
      <td className="px-4 py-3">
        <Skeleton variant="rectangular" height="20px" width="20px" />
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-3">
          <Skeleton variant="rectangular" height="48px" width="48px" />
          <div className="flex-1">
            <Skeleton variant="text" height="16px" width="200px" className="mb-1" />
            <Skeleton variant="text" height="14px" width="150px" />
          </div>
        </div>
      </td>
      <td className="px-4 py-3">
        <Skeleton variant="rounded" height="24px" width="80px" />
      </td>
      <td className="px-4 py-3">
        <Skeleton variant="text" height="14px" width="100px" />
      </td>
      <td className="px-4 py-3">
        <Skeleton variant="text" height="14px" width="120px" />
      </td>
      <td className="px-4 py-3 text-right">
        <div className="flex justify-end gap-2">
          <Skeleton variant="rectangular" height="32px" width="32px" />
          <Skeleton variant="rectangular" height="32px" width="32px" />
        </div>
      </td>
    </tr>
  );
}

/**
 * Grid of project card skeletons
 */
export function ProjectGridSkeleton({ count = 8 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
      {Array.from({ length: count }, (_, i) => (
        <ProjectCardSkeleton key={i} />
      ))}
    </div>
  );
}

/**
 * List of project item skeletons
 */
export function ProjectListSkeleton({ count = 10 }: { count?: number }) {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
            <tr>
              <th className="px-4 py-3 text-left">
                <Skeleton variant="rectangular" height="20px" width="20px" />
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                Project
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                Status
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                Category
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                Updated
              </th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {Array.from({ length: count }, (_, i) => (
              <ProjectListItemSkeleton key={i} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
