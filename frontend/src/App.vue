<template>
  <el-container class="app-container">
    <el-aside width="240px" class="aside-container">
      <div class="logo">
        <el-icon><Crane /></el-icon>
        <span>塔吊安全监控</span>
      </div>
      <el-menu
        :default-active="activeMenu"
        class="menu"
        background-color="#001529"
        text-color="#cfd8dc"
        active-text-color="#409EFF"
        router
      >
        <el-menu-item index="/dashboard">
          <el-icon><DataAnalysis /></el-icon>
          <span>监控中心</span>
        </el-menu-item>
        <el-menu-item index="/persons">
          <el-icon><User /></el-icon>
          <span>人员管理</span>
        </el-menu-item>
        <el-menu-item index="/tower-cranes">
          <el-icon><Tools /></el-icon>
          <span>塔吊管理</span>
        </el-menu-item>
        <el-menu-item index="/lifting-tasks">
          <el-icon><List /></el-icon>
          <span>吊装任务</span>
        </el-menu-item>
        <el-menu-item index="/alarms">
          <el-icon><Warning /></el-icon>
          <span>报警管理</span>
          <el-badge v-if="pendingAlarmCount > 0" :value="pendingAlarmCount" class="menu-badge" />
        </el-menu-item>
        <el-menu-item index="/rectifications">
          <el-icon><Tools /></el-icon>
          <span>整改管理</span>
          <el-badge v-if="openRectificationCount > 0" :value="openRectificationCount" class="menu-badge" />
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header-container">
        <div class="header-title">{{ currentTitle }}</div>
        <div class="header-right">
          <el-tag type="success" effect="dark">系统正常</el-tag>
          <span class="user-info">
            <el-icon><Avatar /></el-icon>
            管理员
          </span>
        </div>
      </el-header>
      <el-main class="main-container">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElNotification } from 'element-plus'
import { useAppStore } from '@/store/app'

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()

const activeMenu = computed(() => route.path)
const pendingAlarmCount = ref(0)
const openRectificationCount = ref(0)

const menuTitles: Record<string, string> = {
  '/dashboard': '监控中心',
  '/persons': '人员管理',
  '/tower-cranes': '塔吊管理',
  '/lifting-tasks': '吊装任务管理',
  '/alarms': '报警管理',
  '/rectifications': '整改管理'
}

const currentTitle = computed(() => {
  return menuTitles[route.path] || '塔吊安全监控系统'
})

const fetchBadgeCounts = async () => {
  try {
    await appStore.fetchDashboardStats()
    pendingAlarmCount.value = appStore.stats?.pendingAlarms || 0
    openRectificationCount.value = appStore.stats?.openRectifications || 0
    if (pendingAlarmCount.value > 0) {
      ElNotification.warning({
        title: '待处理报警',
        message: `当前有 ${pendingAlarmCount.value} 条报警待处理`,
        duration: 3000
      })
    }
  } catch (e) {
    console.error('获取统计数据失败', e)
  }
}

onMounted(() => {
  fetchBadgeCounts()
  setInterval(fetchBadgeCounts, 60000)
})
</script>

<style lang="scss">
.app-container {
  height: 100vh;
  width: 100%;
}

.aside-container {
  background: #001529;
  overflow: hidden;

  .logo {
    height: 64px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    color: #fff;
    font-size: 18px;
    font-weight: 600;
    background: #000c17;
    border-bottom: 1px solid #1f3a5b;

    .el-icon {
      font-size: 24px;
      color: #409EFF;
    }
  }

  .menu {
    border-right: none;
    height: calc(100vh - 64px);
    overflow-y: auto;

    .el-menu-item {
      position: relative;
      margin: 4px 0;
      border-radius: 0;
    }

    .menu-badge {
      margin-left: 8px;
    }
  }
}

.header-container {
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  border-bottom: 1px solid #ebeef5;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);

  .header-title {
    font-size: 18px;
    font-weight: 600;
    color: #303133;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 20px;

    .user-info {
      display: flex;
      align-items: center;
      gap: 6px;
      color: #606266;
    }
  }
}

.main-container {
  background: #f0f2f5;
  padding: 20px;
  overflow-y: auto;
}
</style>
