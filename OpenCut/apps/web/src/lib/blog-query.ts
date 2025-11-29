import type {
  MarbleAuthorList,
  MarbleCategoryList,
  MarblePost,
  MarblePostList,
  MarbleTagList,
} from "@/types/post";
import { unified } from "unified";
import rehypeParse from "rehype-parse";
import rehypeStringify from "rehype-stringify";
import rehypeSlug from "rehype-slug";
import rehypeAutolinkHeadings from "rehype-autolink-headings";
import rehypeSanitize from "rehype-sanitize";

const url =
  process.env.NEXT_PUBLIC_MARBLE_API_URL ?? "https://api.marblecms.com";
const key = process.env.MARBLE_WORKSPACE_KEY ?? "cmd4iw9mm0006l804kwqv0k46";

// Flag to enable offline build mode (skips external API calls during build)
const isOfflineBuild = process.env.OPENCUT_OFFLINE_BUILD === "true";

async function fetchFromMarble<T>(
  endpoint: string,
  fallbackValue?: T
): Promise<T> {
  // In offline build mode, return fallback value immediately
  if (isOfflineBuild && fallbackValue !== undefined) {
    console.log(
      `[Offline Build] Skipping fetch for ${endpoint}, using fallback`
    );
    return fallbackValue;
  }

  try {
    const response = await fetch(`${url}/${key}/${endpoint}`);
    if (!response.ok) {
      throw new Error(
        `Failed to fetch ${endpoint}: ${response.status} ${response.statusText}`
      );
    }
    return (await response.json()) as T;
  } catch (error) {
    console.error(`Error fetching ${endpoint}:`, error);
    // Return fallback value on error if provided (for graceful degradation)
    if (fallbackValue !== undefined) {
      console.log(`[Blog Query] Using fallback value for ${endpoint}`);
      return fallbackValue;
    }
    throw error;
  }
}

export async function getPosts(): Promise<MarblePostList> {
  return fetchFromMarble<MarblePostList>("posts", { posts: [] });
}

export async function getTags(): Promise<MarbleTagList> {
  return fetchFromMarble<MarbleTagList>("tags", { tags: [] });
}

export async function getSinglePost(slug: string): Promise<MarblePost | null> {
  try {
    return await fetchFromMarble<MarblePost>(`posts/${slug}`);
  } catch {
    return null;
  }
}

export async function getCategories(): Promise<MarbleCategoryList> {
  return fetchFromMarble<MarbleCategoryList>("categories", { categories: [] });
}

export async function getAuthors(): Promise<MarbleAuthorList> {
  return fetchFromMarble<MarbleAuthorList>("authors", { authors: [] });
}

export async function processHtmlContent(html: string): Promise<string> {
  const processor = unified()
    .use(rehypeSanitize)
    .use(rehypeParse, { fragment: true })
    .use(rehypeSlug)
    .use(rehypeAutolinkHeadings, { behavior: "append" })
    .use(rehypeStringify);

  const file = await processor.process(html);
  return String(file);
}
