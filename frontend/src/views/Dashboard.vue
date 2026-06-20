<template>
  <div class="page-container">
    <el-row :gutter="16">
      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="card-stat">
            <div class="stat-label">塔吊总数</div>
            <div class="stat-value">
              <span style="color:#409EFF">{{ stats?.totalCranes ?? 0 }}</span>
              <span class="stat-unit">台</span>
            </div>
            <el-progress :percentage="craneWorkingPercent" :color="'#409EFF'" :stroke-width="6" :show-text="false" style="margin-top:12px" />
            <div style="display:flex;justify-content:space-between;margin-top:6px;font-size:12px;color:#909399">
              <span>作业中: <b style="color:#409EFF">{{ stats?.workingCranes ?? 0 }}</b></span>
              <span>预警: <b style="color:#E6A23C">{{ stats?.warningCranes ?? 0 }}</b></span>
              <span>维护: <b>{{ stats?.maintenanceCranes ?? 0 }}</b></span>
            </div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="card-stat">
            <div class="stat-label">司机资质</div>
            <div class="stat-value">
              <span style="color:#67C23A">{{ stats?.qualifiedDrivers ?? 0 }}</span>
              <span class="stat-unit">/{{ stats?.totalDrivers ?? 0 }} 人</span>
            </div>
            <el-progress
              :percentage="driverQualifiedPercent"
              :status="driverQualifiedPercent < 80 ? 'exception' : 'success'"
              :stroke-width="6"
              :show-text="false"
              style="margin-top:12px"
            />
            <div style="display:flex;justify-content:space-between;margin-top:6px;font-size:12px;color:#909399">
              <span>待续期: <b style="color:#E6A23C">{{ stats?.driversWithExpiringCertificates ?? 0 }}</b></span>
              <span>合格率: <b>{{ driverQualifiedPercent }}%</b></span>
            </div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="card-stat">
            <div class="stat-label">今日任务</div>
            <div class="stat-value">
              <span style="color:#E6A23C">{{ stats?.inProgressTasks ?? 0 }}</span>
              <span class="stat-unit">/{{ stats?.totalTasksToday ?? 0 }} 个</span>
            </div>
            <el-progress :percentage="taskProgressPercent" :color="'#E6A23C'" :stroke-width="6" :show-text="false" style="margin-top:12px" />
            <div style="display:flex;justify-content:space-between;margin-top:6px;font-size:12px;color:#909399">
              <span>已完成: <b style="color:#67C23A">{{ stats?.completedTasksToday ?? 0 }}</b></span>
              <span>进度: <b>{{ taskProgressPercent }}%</b></span>
            </div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="stat-card">
          <div class="card-stat">
            <div class="stat-label">安全风险</div>
            <div class="stat-value">
              <span :style="{ color: stats && stats.criticalAlarms > 0 ? '#F56C6C' : '#67C23A' }">
                {{ stats?.pendingAlarms ?? 0 }}
              </span>
              <span class="stat-unit">待处理</span>
            </div>
            <el-progress
              :percentage="riskPercent"
              :status="stats && stats.criticalAlarms > 0 ? 'exception' : 'success'"
              :stroke-width="6"
              :show-text="false"
              style="margin-top:12px"
            />
            <div style="display:flex;justify-content:space-between;margin-top:6px;font-size:12px;color:#909399">
              <span>严重报警: <b style="color:#F56C6C">{{ stats?.criticalAlarms ?? 0 }}</b></span>
              <span>待整改: <b style="color:#E6A23C">{{ stats?.openRectifications ?? 0 }}</b></span>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :span="14">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">报警趋势分析（近7天）</span>
              <el-tag size="small" type="warning">实时联动黑匣子</el-tag>
            </div>
          </template>
          <div class="chart-container">
            <v-chart class="chart" :option="alarmTrendOption" autoresize />
          </div>
        </el-card>
      </el-col>

      <el-col :span="10">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">报警类型分布</span>
              <el-tag size="small">按严重程度排序</el-tag>
            </div>
          </template>
          <div class="chart-container">
            <v-chart class="chart" :option="alarmTypeOption" autoresize />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">任务状态分布</span>
              <el-tag size="small" type="success">实时更新</el-tag>
            </div>
          </template>
          <div class="chart-container">
            <v-chart class="chart" :option="taskStatusOption" autoresize />
          </div>
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">塔吊工作量（近7天）</span>
              <el-tag size="small" type="info">任务完成数 + 报警数</el-tag>
            </div>
          </template>
          <div class="chart-container">
            <v-chart class="chart" :option="craneWorkloadOption" autoresize />
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">
                <el-icon style="color:#F56C6C"><Warning /></el-icon>
                待处理报警
                <el-badge v-if="pendingAlarms.length" :value="pendingAlarms.length" class="inline-badge" />
              </span>
              <el-button type="primary" link @click="$router.push('/alarms')">查看全部</el-button>
            </div>
          </template>
          <el-table :data="pendingAlarms.slice(0, 5)" size="small" stripe style="width:100%">
            <el-table-column label="编号" prop="id" width="70" align="center" />
            <el-table-column label="报警类型" width="110">
              <template #default="{ row }">
                <el-tag :type="AlarmTypeMap[row.alarmType].type" size="small">
                  {{ AlarmTypeMap[row.alarmType].label }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="级别" width="80" align="center">
              <template #default="{ row }">
                <el-tag :type="AlarmLevelMap[row.alarmLevel].type" size="small" effect="dark">
                  {{ AlarmLevelMap[row.alarmLevel].label }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="塔吊" prop="towerCrane.craneNo" width="90" align="center" />
            <el-table-column label="描述" prop="description" show-overflow-tooltip />
            <el-table-column label="时间" width="160">
              <template #default="{ row }">{{ formatTime(row.alarmTime) }}</template>
            </el-table-column>
            <el-table-column label="状态" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="AlarmStatusMap[row.status].type" size="small">
                  {{ AlarmStatusMap[row.status].label }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card shadow="hover">
          <template #header>
            <div style="display:flex;justify-content:space-between;align-items:center">
              <span style="font-weight:600;font-size:15px">
                <el-icon style="color:#E6A23C"><Tools /></el-icon>
                待完成整改
                <el-badge v-if="openRectifications.length" :value="openRectifications.length" type="warning" class="inline-badge" />
              </span>
              <el-button type="primary" link @click="$router.push('/rectifications')">查看全部</el-button>
            </div>
          </template>
          <el-table :data="openRectifications.slice(0, 5)" size="small" stripe style="width:100%">
            <el-table-column label="编号" prop="rectificationNo" width="140" align="center" />
            <el-table-column label="优先级" width="80" align="center">
              <template #default="{ row }">
                <el-tag :type="RectificationPriorityMap[row.priority].type" size="small" effect="dark">
                  {{ RectificationPriorityMap[row.priority].label }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="塔吊" prop="towerCrane.craneNo" width="80" align="center" />
            <el-table-column label="标题" prop="title" show-overflow-tooltip />
            <el-table-column label="状态" width="90" align="center">
              <template #default="{ row }">
                <el-tag :type="RectificationStatusMap[row.status].type" size="small">
                  {{ RectificationStatusMap[row.status].label }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="截止时间" width="160">
              <template #default="{ row }">
                <span :style="{ color: isOverdue(row.deadline, row.status) ? '#F56C6C' : '' }">
                  {{ formatTime(row.deadline) }}
                </span>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAppStore } from '@/store/app'
import VChart from 'vue-echarts'
import * as echarts from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart, PieChart, GaugeChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent
} from 'echarts/components'
import dayjs from 'dayjs'
import {
  AlarmTypeMap,
  AlarmLevelMap,
  AlarmStatusMap,
  RectificationPriorityMap,
  RectificationStatusMap
} from '@/utils/enumMaps'
import type { Alarm, Rectification } from '@/api'
import api from '@/api'

echarts.use([
  CanvasRenderer,
  LineChart,
  BarChart,
  PieChart,
  GaugeChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent
])

const appStore = useAppStore()

const stats = computed(() => appStore.stats)
const pendingAlarms = ref<Alarm[]>([])
const openRectifications = ref<Rectification[]>([])

const craneWorkingPercent = computed(() => {
  const total = stats.value?.totalCranes || 1
  return Math.round(((stats.value?.workingCranes || 0) / total) * 100)
})

const driverQualifiedPercent = computed(() => {
  const total = stats.value?.totalDrivers || 1
  return Math.round(((stats.value?.qualifiedDrivers || 0) / total) * 100)
})

const taskProgressPercent = computed(() => {
  const total = stats.value?.totalTasksToday || 1
  const done = stats.value?.completedTasksToday || 0
  const inProgress = stats.value?.inProgressTasks || 0
  return Math.round(((done + inProgress * 0.5) / total) * 100)
})

const riskPercent = computed(() => {
  const pending = stats.value?.pendingAlarms || 0
  const open = stats.value?.openRectifications || 0
  const urgent = stats.value?.urgentRectifications || 0
  const critical = stats.value?.criticalAlarms || 0
  const base = pending + open + urgent * 2 + critical * 3
  return Math.min(base * 5, 100)
})

const alarmTrendOption = computed(() => {
  const data = appStore.alarmTrend
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['总报警数', '严重', '警告', '已解决'], bottom: 0 },
    grid: { left: '3%', right: '4%', bottom: '12%', top: '10%', containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: data.map(d => dayjs(d.date).format('MM-DD'))
    },
    yAxis: { type: 'value', minInterval: 1 },
    series: [
      {
        name: '总报警数', type: 'line', smooth: true, stack: 'Total',
        areaStyle: { opacity: 0.3 },
        lineStyle: { width: 3 },
        data: data.map(d => d.totalAlarms),
        itemStyle: { color: '#409EFF' }
      },
      {
        name: '严重', type: 'line', smooth: true,
        lineStyle: { width: 2, type: 'dashed' },
        data: data.map(d => d.criticalAlarms),
        itemStyle: { color: '#F56C6C' }
      },
      {
        name: '警告', type: 'line', smooth: true,
        lineStyle: { width: 2, type: 'dashed' },
        data: data.map(d => d.warningAlarms),
        itemStyle: { color: '#E6A23C' }
      },
      {
        name: '已解决', type: 'bar', barWidth: 16,
        data: data.map(d => d.resolvedAlarms),
        itemStyle: { color: '#67C23A', borderRadius: [4, 4, 0, 0] }
      }
    ]
  }
})

const alarmTypeOption = computed(() => {
  const data = appStore.alarmTypeDistribution
  return {
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { orient: 'vertical', right: '5%', top: 'center' },
    color: ['#F56C6C', '#E6A23C', '#F56C6C', '#E6A23C', '#F56C6C', '#E6A23C', '#F56C6C'],
    series: [{
      type: 'pie',
      radius: ['45%', '72%'],
      center: ['35%', '50%'],
      avoidLabelOverlap: false,
      itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
      label: { show: false, position: 'center' },
      emphasis: {
        label: { show: true, fontSize: 18, fontWeight: 'bold', formatter: '{b}\n{c}次' }
      },
      data: data.map(d => ({
        name: AlarmTypeMap[d.type]?.label || '其他',
        value: d.count
      }))
    }]
  }
})

const taskStatusOption = computed(() => {
  const data = appStore.taskStatusDistribution
  const statusLabels = ['草稿', '待司机确认', '进行中', '已完成', '已取消', '已暂停']
  const statusColors = ['#909399', '#E6A23C', '#409EFF', '#67C23A', '#909399', '#F56C6C']
  return {
    tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
    legend: { bottom: 0 },
    color: statusColors,
    series: [{
      type: 'pie',
      radius: '65%',
      center: ['50%', '45%'],
      itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
      label: { formatter: '{b}\n{c}', fontSize: 12 },
      data: statusLabels.map((label, idx) => {
        const item = data.find(d => d.status === idx + 1)
        return { value: item?.count || 0, name: label }
      }).filter(i => i.value > 0)
    }]
  }
})

const craneWorkloadOption = computed(() => {
  const data = appStore.craneWorkload
  return {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    legend: { data: ['完成任务', '进行中', '报警次数'], bottom: 0 },
    grid: { left: '3%', right: '4%', bottom: '12%', top: '10%', containLabel: true },
    xAxis: { type: 'category', data: data.map(d => d.craneNo) },
    yAxis: { type: 'value', minInterval: 1 },
    series: [
      {
        name: '完成任务', type: 'bar', stack: 'tasks', barWidth: 24,
        data: data.map(d => d.completedTasks),
        itemStyle: { color: '#67C23A', borderRadius: [4, 4, 0, 0] }
      },
      {
        name: '进行中', type: 'bar', stack: 'tasks', barWidth: 24,
        data: data.map(d => d.inProgressTasks),
        itemStyle: { color: '#409EFF' }
      },
      {
        name: '报警次数', type: 'line', smooth: true,
        lineStyle: { width: 3 }, symbol: 'circle', symbolSize: 8,
        data: data.map(d => d.totalAlarms),
        itemStyle: { color: '#F56C6C' }
      }
    ]
  }
})

const formatTime = (t: string) => dayjs(t).format('YYYY-MM-DD HH:mm')

const isOverdue = (deadline: string, status: number) => {
  return dayjs(deadline).isBefore(dayjs()) && status !== 4
}

const fetchAllData = async () => {
  try {
    await Promise.all([
      appStore.fetchDashboardStats(),
      appStore.fetchAlarmTrend(7),
      appStore.fetchTaskStatusDistribution(),
      appStore.fetchCraneWorkload(),
      appStore.fetchAlarmTypeDistribution()
    ])
    const [alarmsRes, rectRes] = await Promise.all([
      api.alarms.getPending(),
      api.rectifications.getOpen()
    ])
    pendingAlarms.value = alarmsRes.data
    openRectifications.value = rectRes.data
  } catch (e) {
    console.error('加载数据失败', e)
  }
}

onMounted(fetchAllData)

watch(
  () => stats.value?.pendingAlarms,
  (newVal, oldVal) => {
    if (newVal !== undefined && oldVal !== undefined && newVal > oldVal) {
      fetchAllData()
    }
  }
)
</script>

<style lang="scss" scoped>
.stat-card {
  .el-card__body {
    padding: 20px;
  }
}

.chart-container {
  width: 100%;
  height: 340px;
}

.chart {
  width: 100%;
  height: 100%;
}

.inline-badge {
  margin-left: 8px;
  vertical-align: middle;
}
</style>
