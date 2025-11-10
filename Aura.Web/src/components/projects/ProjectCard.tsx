import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  MoreVertical,
  Play,
  Trash2,
  Copy,
  Edit,
  Download,
  Clock,
  Folder,
  Tag,
} from 'lucide-react';
import { Project } from '../../api/projectManagement';
import { formatDistanceToNow } from 'date-fns';

interface ProjectCardProps {
  project: Project;
  selected: boolean;
  onSelect: (selected: boolean) => void;
  onDelete: () => void;
  onDuplicate: () => void;
}

export function ProjectCard({
  project,
  selected,
  onSelect,
  onDelete,
  onDuplicate,
}: ProjectCardProps) {
  const [showMenu, setShowMenu] = useState(false);

  const statusColors = {
    Draft: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    InProgress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    Completed: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    Failed: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    Cancelled: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  };

  return (
    <div
      className={`relative bg-white dark:bg-gray-800 rounded-lg border-2 transition-all duration-200 hover:shadow-lg group ${
        selected
          ? 'border-blue-500 dark:border-blue-400'
          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
      }`}
    >
      {/* Checkbox */}
      <div className="absolute top-3 left-3 z-10">
        <input
          type="checkbox"
          checked={selected}
          onChange={(e) => onSelect(e.target.checked)}
          className="w-4 h-4 rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 focus:ring-2"
          onClick={(e) => e.stopPropagation()}
        />
      </div>

      {/* Menu button */}
      <div className="absolute top-3 right-3 z-10">
        <button
          onClick={(e) => {
            e.stopPropagation();
            setShowMenu(!showMenu);
          }}
          className="p-1 rounded-lg bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm opacity-0 group-hover:opacity-100 transition-opacity hover:bg-gray-100 dark:hover:bg-gray-700"
        >
          <MoreVertical className="w-4 h-4" />
        </button>

        {showMenu && (
          <>
            <div
              className="fixed inset-0 z-10"
              onClick={() => setShowMenu(false)}
            />
            <div className="absolute right-0 mt-1 w-48 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-20 overflow-hidden">
              <Link
                to={`/projects/${project.id}`}
                className="flex items-center px-4 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                <Edit className="w-4 h-4 mr-3" />
                Open
              </Link>
              <button
                onClick={() => {
                  onDuplicate();
                  setShowMenu(false);
                }}
                className="w-full flex items-center px-4 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                <Copy className="w-4 h-4 mr-3" />
                Duplicate
              </button>
              {project.outputFilePath && (
                <a
                  href={project.outputFilePath}
                  download
                  className="flex items-center px-4 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-700"
                >
                  <Download className="w-4 h-4 mr-3" />
                  Download
                </a>
              )}
              <div className="border-t border-gray-200 dark:border-gray-700" />
              <button
                onClick={() => {
                  onDelete();
                  setShowMenu(false);
                }}
                className="w-full flex items-center px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
              >
                <Trash2 className="w-4 h-4 mr-3" />
                Delete
              </button>
            </div>
          </>
        )}
      </div>

      <Link to={`/projects/${project.id}`} className="block">
        {/* Thumbnail */}
        <div className="aspect-video bg-gradient-to-br from-blue-500 to-purple-600 rounded-t-lg relative overflow-hidden">
          {project.thumbnailPath ? (
            <img
              src={project.thumbnailPath}
              alt={project.title}
              className="w-full h-full object-cover"
            />
          ) : (
            <div className="flex items-center justify-center h-full">
              <Folder className="w-16 h-16 text-white/30" />
            </div>
          )}

          {/* Status badge */}
          <div className="absolute top-3 left-3">
            <span
              className={`px-2 py-1 text-xs font-medium rounded-full ${
                statusColors[project.status]
              }`}
            >
              {project.status}
            </span>
          </div>

          {/* Progress bar for in-progress projects */}
          {project.status === 'InProgress' && project.progressPercent > 0 && (
            <div className="absolute bottom-0 left-0 right-0 h-1 bg-black/20">
              <div
                className="h-full bg-blue-500 transition-all duration-300"
                style={{ width: `${project.progressPercent}%` }}
              />
            </div>
          )}
        </div>

        {/* Content */}
        <div className="p-4">
          <h3 className="font-semibold text-gray-900 dark:text-white mb-1 truncate">
            {project.title}
          </h3>

          {project.description && (
            <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2 mb-3">
              {project.description}
            </p>
          )}

          {/* Metadata */}
          <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400 mb-3">
            {project.durationSeconds && (
              <div className="flex items-center gap-1">
                <Clock className="w-3 h-3" />
                {Math.floor(project.durationSeconds / 60)}:{String(Math.floor(project.durationSeconds % 60)).padStart(2, '0')}
              </div>
            )}
            {project.sceneCount > 0 && (
              <span>{project.sceneCount} scenes</span>
            )}
            {project.category && (
              <div className="flex items-center gap-1">
                <Folder className="w-3 h-3" />
                {project.category}
              </div>
            )}
          </div>

          {/* Tags */}
          {project.tags && project.tags.length > 0 && (
            <div className="flex flex-wrap gap-1 mb-3">
              {project.tags.slice(0, 3).map((tag, index) => (
                <span
                  key={index}
                  className="px-2 py-0.5 text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded"
                >
                  {tag}
                </span>
              ))}
              {project.tags.length > 3 && (
                <span className="px-2 py-0.5 text-xs text-gray-500 dark:text-gray-400">
                  +{project.tags.length - 3}
                </span>
              )}
            </div>
          )}

          {/* Footer */}
          <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 pt-3 border-t border-gray-100 dark:border-gray-700">
            <span>
              Updated {formatDistanceToNow(new Date(project.updatedAt), { addSuffix: true })}
            </span>
            {project.status === 'Completed' && (
              <button className="flex items-center gap-1 text-blue-600 dark:text-blue-400 hover:underline">
                <Play className="w-3 h-3" />
                View
              </button>
            )}
          </div>
        </div>
      </Link>
    </div>
  );
}
