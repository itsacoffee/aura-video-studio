import { useQuery } from '@tanstack/react-query';
import { projectManagementApi } from '../../api/projectManagement';

interface ProjectFiltersProps {
  filters: {
    status: string;
    category: string;
    tags: string;
    sortBy: string;
    ascending: boolean;
  };
  onFilterChange: (filters: Partial<ProjectFiltersProps['filters']>) => void;
}

export function ProjectFilters({ filters, onFilterChange }: ProjectFiltersProps) {
  const { data: categoriesData } = useQuery({
    queryKey: ['categories'],
    queryFn: () => projectManagementApi.getCategories(),
  });

  const { data: tagsData } = useQuery({
    queryKey: ['tags'],
    queryFn: () => projectManagementApi.getTags(),
  });

  const categories = categoriesData?.categories || [];
  const tags = tagsData?.tags || [];

  return (
    <div className="p-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* Status filter */}
      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Status
        </label>
        <select
          value={filters.status}
          onChange={(e) => onFilterChange({ status: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          <option value="">All</option>
          <option value="Draft">Draft</option>
          <option value="InProgress">In Progress</option>
          <option value="Completed">Completed</option>
          <option value="Failed">Failed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </div>

      {/* Category filter */}
      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Category
        </label>
        <select
          value={filters.category}
          onChange={(e) => onFilterChange({ category: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          <option value="">All</option>
          {categories.map((category) => (
            <option key={category} value={category}>
              {category}
            </option>
          ))}
        </select>
      </div>

      {/* Sort by */}
      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Sort by
        </label>
        <select
          value={filters.sortBy}
          onChange={(e) => onFilterChange({ sortBy: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          <option value="UpdatedAt">Last Updated</option>
          <option value="CreatedAt">Date Created</option>
          <option value="Title">Name</option>
          <option value="Status">Status</option>
          <option value="Category">Category</option>
        </select>
      </div>

      {/* Sort direction */}
      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Order
        </label>
        <select
          value={filters.ascending ? 'asc' : 'desc'}
          onChange={(e) => onFilterChange({ ascending: e.target.value === 'asc' })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        >
          <option value="desc">Descending</option>
          <option value="asc">Ascending</option>
        </select>
      </div>
    </div>
  );
}
