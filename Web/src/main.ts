import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import { setupPermissionDirective } from './auth/permission'
import { setupNaiveDiscreteApi } from './lib/naive'
import router from './router'
import './styles/base.css'
import './styles/nprogress.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

setupNaiveDiscreteApi()
setupPermissionDirective(app)

app.mount('#app')
