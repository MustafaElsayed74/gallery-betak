# Deployment Strategy & Disaster Recovery Plan

This document outlines the deployment strategy, server setup, and disaster recovery procedures for the Gallery Betak E-Commerce Platform.

## 1. Server Setup Architecture

We recommend a High-Availability (HA) cloud environment on providers like AWS, Azure, or DigitalOcean, depending on the budget. For moderate traffic scaling, the following setup is optimal:

### Environment Stack
- **Database Server**: Managed SQL Server or SQL Server on a dedicated VM with SSDs.
- **Cache Server**: Managed Redis (e.g., Azure Cache for Redis, AWS ElastiCache) to offload cart merging and caching states safely away from API nodes.
- **Compute Nodes**: A load balancer targeting docker containers. This can be orchestrated via Docker Swarm, Kubernetes, or managed PaaS (like AWS ECS/Azure App Services) to dynamically provision resources.
- **Blob Storage / CDN**: Instead of serving static product images via `StaticFiles()`, upload images locally first but stream them to an S3-compatible Blob storage (AWS S3 or Cloudflare R2) integrated with a Global CDN for faster global and regional load times.

## 2. Reverse Proxy & SSL Configuration

For external traffic, direct access should never hit the internal application stack.

### Nginx (or Cloudflare Edge)
- Use an Nginx reverse proxy instance to route inbound host headers to `api` (8080) and `frontend` (80).
- Terminate SSL certificates directly at the Nginx edge or via a Load Balancer.
- Use **Let's Encrypt** automation (like `certbot`) which is perfectly compliant and cost-effective.
- Force `HTTPS` redirect out-of-the-box in the Nginx config.
- `SecurityHeadersMiddleware.cs` is already handling HSTS tracking limits.

## 3. Backup Strategy

To prevent data loss, the following protocol is mandatory:

### Database Storage
- **Daily Full Backups**: Conduct a full automated snapshot of the SQL Database at 03:00 AM AST.
- **Transaction Logs**: Configure log backups every 15 minutes to allow point-in-time recovery during peak sale hours.
- **Retention**: Keep full backups for 30 days stored off-site (e.g., AWS S3 lifecycle policy to Glacier).

### Application State
- No application state is saved locally. Redis handles session and cart caches. Redis data is transient. No backups are strictly necessary unless persistence is prioritized out-of-band.

## 4. Disaster Recovery (DR) Plan

Given an outage, evaluate Recovery Point Objective (RPO) and Recovery Time Objective (RTO).

### Scenario A: Compute Node Failure (API/Frontend)
- **Response**: The orchestrator (Kubernetes/ECS or Docker Compose with `restart: always`) should automatically kill the unhealthy instance and spin up a new container image from the registry.
- **RTO**: ~2 minutes.

### Scenario B: Database Catastrophe
- **Response**: Restore the last known good active transaction log off the daily backup snapshot. Rotate secrets if a breach.
- **RTO**: ~30-60 minutes depending on data mass size.
- **RPO**: 15 minutes max data loss during business hours assuming transaction logs execute cleanly.
