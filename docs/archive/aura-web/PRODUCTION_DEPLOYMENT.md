> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---

# Production Deployment Configuration Guide

## Static Asset Caching Headers

When deploying the Aura Video Studio frontend to production, configure the following HTTP cache headers for optimal performance:

### For hashed assets (`/assets/*-[hash].js`, `/assets/*-[hash].css`, etc.)
These files have content hashes in their names and can be cached indefinitely:

```
Cache-Control: public, max-age=31536000, immutable
```

### For index.html
This file should not be cached to ensure users always get the latest version:

```
Cache-Control: no-cache, no-store, must-revalidate
Pragma: no-cache
Expires: 0
```

### For other static assets without hashes
Apply moderate caching:

```
Cache-Control: public, max-age=3600
```

## Compression

The build process generates pre-compressed `.gz` (gzip) and `.br` (brotli) files for all assets.

### Nginx Configuration Example

```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    root /var/www/aura-web/dist;
    index index.html;
    
    # Enable gzip compression for non-pre-compressed files
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 1000;
    
    # Serve pre-compressed brotli files if available
    location ~ \.(js|css|svg|json)$ {
        gzip_static on;
        brotli_static on;
        
        # Cache hashed assets for 1 year
        location ~ \-[a-f0-9]{8,}\.(js|css)$ {
            add_header Cache-Control "public, max-age=31536000, immutable";
        }
    }
    
    # Don't cache index.html
    location = /index.html {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
        add_header Pragma "no-cache";
        add_header Expires "0";
    }
    
    # SPA routing - serve index.html for all routes
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

### Apache Configuration Example

```apache
<VirtualHost *:80>
    ServerName your-domain.com
    DocumentRoot /var/www/aura-web/dist
    
    <Directory /var/www/aura-web/dist>
        # Enable mod_rewrite for SPA routing
        RewriteEngine On
        RewriteBase /
        RewriteRule ^index\.html$ - [L]
        RewriteCond %{REQUEST_FILENAME} !-f
        RewriteCond %{REQUEST_FILENAME} !-d
        RewriteRule . /index.html [L]
        
        # Serve pre-compressed files
        AddEncoding gzip .gz
        AddEncoding br .br
        
        # Cache hashed assets for 1 year
        <FilesMatch "\-[a-f0-9]{8,}\.(js|css)$">
            Header set Cache-Control "public, max-age=31536000, immutable"
        </FilesMatch>
        
        # Don't cache index.html
        <Files "index.html">
            Header set Cache-Control "no-cache, no-store, must-revalidate"
            Header set Pragma "no-cache"
            Header set Expires "0"
        </Files>
    </Directory>
</VirtualHost>
```

## Environment Variables

Ensure the following environment variables are set in production:

- `VITE_ENV=production`
- `VITE_ENABLE_DEBUG=false`
- `VITE_ENABLE_DEV_TOOLS=false`
- `VITE_API_BASE_URL=/api` (or your production API URL)

These are configured in `.env.production` which is automatically loaded when building with `--mode production`.

## Security Headers

Consider adding these security headers to your production server:

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
```

## Source Maps

Source maps are generated as "hidden" source maps in production builds. They exist in the `dist/assets` directory but are not referenced in the JavaScript files served to end users. This allows:

- Debugging in production if needed by manually uploading source maps to error tracking services
- Reduced bandwidth as source maps are not downloaded by users
- Protection of source code from casual inspection

To use source maps with an error tracking service (e.g., Sentry), upload the `.map` files to the service after deployment and exclude them from being served to end users.
