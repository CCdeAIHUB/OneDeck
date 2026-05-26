import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('@/views/DashboardView.vue'),
      meta: { title: '仪表盘', icon: 'solar:home-bold' },
    },
    {
      path: '/devices',
      name: 'devices',
      component: () => import('@/views/DevicesView.vue'),
      meta: { title: '设备', icon: 'solar:smartphone-bold' },
    },
    {
      path: '/devices/:id',
      name: 'device-detail',
      component: () => import('@/views/DeviceDetailView.vue'),
      meta: { title: '设备详情', icon: 'solar:smartphone-bold' },
    },
    {
      path: '/schemes',
      name: 'schemes',
      component: () => import('@/views/SchemesView.vue'),
      meta: { title: '方案', icon: 'solar:widget-bold' },
    },
    {
      path: '/schemes/:id/editor',
      name: 'scheme-editor',
      component: () => import('@/views/SchemeEditorView.vue'),
      meta: { title: '方案编辑器', icon: 'solar:widget-bold' },
    },
    {
      path: '/plugins',
      name: 'plugins',
      component: () => import('@/views/PluginsView.vue'),
      meta: { title: '插件', icon: 'solar:widget-2-bold' },
    },
    {
      path: '/logs',
      name: 'logs',
      component: () => import('@/views/LogsView.vue'),
      meta: { title: '日志', icon: 'solar:document-text-bold' },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { title: '设置', icon: 'solar:settings-bold' },
    },
  ],
})

export default router
