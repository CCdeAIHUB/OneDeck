import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/', redirect: '/dashboard' },
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
      meta: { title: '设备详情' },
    },
    {
      path: '/pages',
      name: 'pages',
      component: () => import('@/views/PagesView.vue'),
      meta: { title: '页面', icon: 'solar:document-bold' },
    },
    {
      path: '/pages/:id/designer',
      name: 'page-designer',
      component: () => import('@/views/PageDesignerView.vue'),
      meta: { title: '页面设计' },
    },
    {
      path: '/components',
      name: 'components',
      component: () => import('@/views/ComponentsView.vue'),
      meta: { title: '组件', icon: 'solar:widget-2-bold' },
    },
    {
      path: '/components/:id/designer',
      name: 'component-designer',
      component: () => import('@/views/ComponentDesignerView.vue'),
      meta: { title: '组件设计' },
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
      meta: { title: '方案编辑器' },
    },
    {
      path: '/plugins',
      name: 'plugins',
      component: () => import('@/views/PluginsView.vue'),
      meta: { title: '插件', icon: 'solar:puzzle-bold' },
    },
    {
      path: '/shared-params',
      name: 'shared-params',
      component: () => import('@/views/SharedParamsView.vue'),
      meta: { title: '公共参数库', icon: 'solar:database-bold' },
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
