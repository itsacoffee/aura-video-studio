import { formatDistanceToNow } from 'date-fns';
import { MoreVertical, Trash2, Copy, Edit } from 'lucide-react';
import { useState, memo } from 'react';
import { Link } from 'react-router-dom';
import { Project } from '../../api/projectManagement';

interface ProjectListItemProps {
  project: Project;
  selected: boolean;
  onSelect: (selected: boolean) => void;
  onDelete: () => void;
  onDuplicate: () => void;
}

const ProjectListItemComponent = ({
  project,
  selected,
  onSelect,
  onDelete,
  onDuplicate,
}: ProjectListItemProps) => {
  const [showMenu, setShowMenu] = useState(false);

  const statusColors = {
    Draft: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    InProgress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    Completed: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    Failed: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    Cancelled: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  };

  return (
    <tr className="hover:bg-gray-50 dark:hover:bg-gray-900/50">
      <td className="px-4 py-3">
        <input
          type="checkbox"
          checked={selected}
          onChange={(e) => onSelect(e.target.checked)}
          className="rounded border-gray-300 dark:border-gray-600"
        />
      </td>
      <td className="px-4 py-3">
        <Link
          to={`/projects/${project.id}`}
          className="flex items-center gap-3 hover:text-blue-600 dark:hover:text-blue-400"
        >
          <div className="w-12 h-12 rounded bg-gradient-to-br from-blue-500 to-purple-600 flex-shrink-0" />
          <div className="min-w-0">
            <div className="font-medium text-gray-900 dark:text-white truncate">
              {project.title}
            </div>
            {project.description && (
              <div className="text-sm text-gray-500 dark:text-gray-400 truncate">
                {project.description}
              </div>
            )}
          </div>
        </Link>
      </td>
      <td className="px-4 py-3">
        <span
          className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
            statusColors[project.status]
          }`}
        >
          {project.status}
        </span>
      </td>
      <td className="px-4 py-3">
        <span className="text-sm text-gray-600 dark:text-gray-300">{project.category || '-'}</span>
      </td>
      <td className="px-4 py-3">
        <span className="text-sm text-gray-500 dark:text-gray-400">
          {formatDistanceToNow(new Date(project.updatedAt), { addSuffix: true })}
        </span>
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center justify-end gap-2">
          <Link
            to={`/projects/${project.id}`}
            className="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <Edit className="w-4 h-4" />
          </Link>
          <button
            onClick={onDuplicate}
            className="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <Copy className="w-4 h-4" />
          </button>
          <button
            onClick={onDelete}
            className="p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20 text-red-600 dark:text-red-400"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      </td>
    </tr>
  );
};

export const ProjectListItem = memo(ProjectListItemComponent);
