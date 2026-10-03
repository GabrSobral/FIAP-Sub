import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

export default {
  // Only the standard preprocessor: the components are plain HTML, CSS and JavaScript.
  preprocess: vitePreprocess(),
};
